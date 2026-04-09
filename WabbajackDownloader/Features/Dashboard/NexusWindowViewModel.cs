using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using WabbajackDownloader.Common.Configuration;
using WabbajackDownloader.Features.WebView;

namespace WabbajackDownloader.Features.Dashboard;

// Users may need to manually install WebView2 runtime https://developer.microsoft.com/microsoft-edge/webview2/consumer/
internal partial class NexusWindowViewModel : ObservableObject
{
    private const string NexusLoginUrl = "https://users.nexusmods.com/auth/sign_in?redirect_url=";
    private const string SkyUIUrl = "https://www.nexusmods.com/skyrimspecialedition/mods/12604";
    private const string AccountUrl = "https://users.nexusmods.com/account/security";

    #region Observables
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddressText))]
    public partial Uri Address { get; set; }
    public string AddressText => Address.ToString();

    [ObservableProperty]
    public partial IJavaScriptExecutionEngine JSExecutionEngine { get; set; }

    [ObservableProperty]
    public partial string? DownloadFolderPath { get; set; }
    #endregion

    public NexusWindowViewModel(AppSettings settings, IJavaScriptExecutionEngine executionEngine)
    {
        Address = new Uri(NexusLoginUrl + SkyUIUrl);
        JSExecutionEngine = executionEngine;
        DownloadFolderPath = settings.DownloadFolder;
    }

    #region Commands
    [RelayCommand]
    private async Task NavigationCompleted(Uri address)
    {
        // This means the user has logged in successfully
        if (address.AbsoluteUri == SkyUIUrl || address.AbsoluteUri == AccountUrl)
        {
            Debug.WriteLine("User logged in successfully. Redirecting...");
            Address = new Uri(SkyUIUrl);
        }

        // This means we're probably on a download page
        else if (address.AbsoluteUri.Contains("file_id"))
            await JSExecutionEngine.ExecuteScriptAsync(AutoDownloadScript);
    }
    private const string AutoDownloadScript = """
                ;(function() {
                	function isNexusFilePage() {
                		const url = new URL(location.href);
                		return url.hostname.endsWith('nexusmods.com') &&
                			   url.searchParams.has('file_id');
                	}

                	function clickButton() {
                	  let button;
                	  // find the button in mod file download
                	  const mfd = document.querySelector('mod-file-download') || document.querySelector('[user-is-logged-in="true"]');
                	  if (mfd) {
                		const event = new Event('slowDownload', { bubbles: true, cancelable: true });
                		// dispatch download event directly if possible
                		if (mfd.dispatchEvent(event))
                		  return;
                		// fallback to finding the button in shadow root
                		else if (mfd.shadowRoot)
                		  button = findButtonInBranch(mfd.shadowRoot);
                	  }
                	  // maybe the button is hiding in plain sight
                	  if (!button)
                		button = findButtonInBranch(document);
                	  // last ditch attempt to find the button by looking in shadow roots
                	  if (!button) {
                		const elements = document.querySelectorAll('*');
                		for (const element of elements) {
                		   if (element.shadowRoot) {
                			const buttonFound = findButtonInBranch();
                			if (buttonFound) {
                			  button = buttonFound;
                			  break;
                			}
                		   }
                		}
                	  }
                	  button?.click();
                	}

                	function findButtonInBranch(root) {
                	  const buttons = root.querySelectorAll('button');
                	  for (const button of buttons) {
                		const text = button.textContent.trim().toLowerCase();
                		if (text.includes('slow download'))
                		  return button;
                	  }
                	  return null;
                	}

                	if (!isNexusFilePage()) return;

                	const section = document.querySelector('section.modpage');
                	if (!section) {
                		clickButton();
                		return;
                	}

                	const game_id = section.dataset.gameId;
                	const params = new URLSearchParams(window.location.search);
                	const file_id = params.get('file_id') || params.get('id');
                	if (!file_id || !game_id) {
                		clickButton();
                		return;
                	}

                	if (!window.jQuery || typeof jQuery.ajax !== 'function') {
                		clickButton();
                		return;
                	}
                	$.ajax(
                		{
                			type: "POST",
                			url: "/Core/Libs/Common/Managers/Downloads?GenerateDownloadUrl",
                			data: {
                				fid: file_id,
                				game_id: game_id,
                			},
                			success: function (data) {
                				if (data && data.url) {
                					window.location.href = data.url;
                				} else {
                					clickButton();
                				}
                			},
                			error: function () {
                				clickButton();
                			}
                		}
                	);
                })();
                """;

    [RelayCommand]
    private void DownloadStarting(DownloadStartingEventArgs args)
    {
        args.Handled = true;
        Debug.WriteLine($"Downloading file {args.DownloadOperation.ResultFilePath}");
    }
    #endregion
}