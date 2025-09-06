using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WabbajackDownloader.Core;
using WabbajackDownloader.Exceptions;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Events;

namespace WabbajackDownloader.Cef;

/// <summary>
/// A wrapper around AvaloniaCefBrowser that exposes the underlying CefBrowser.
/// </summary>
internal class AutoDownloadCefBrowser : AvaloniaCefBrowser, IDisposable
{
    public Window Owner { get; }

    public CefBrowser CefBrowser => UnderlyingBrowser;

    public void LoadUrl(string url)
        => UnderlyingBrowser.GetMainFrame().LoadUrl(url);

    private DownloadNothingHandler Handler
        => DownloadHandler as DownloadNothingHandler ?? throw new InvalidHandlerException("Wrong download handler type. Expected type: " + nameof(DownloadNothingHandler));

    private readonly SemaphoreSlim semaphore = new(1);

    private readonly ILogger? logger;

    public AutoDownloadCefBrowser(Window owner, Func<CefRequestContext>? contextFactory, ILogger? logger) : base(contextFactory)
    {
        Owner = owner;
        this.logger = logger;
        DownloadHandler = new DownloadNothingHandler();
        LoadEnd += OnBrowserLoadEnd;
    }

    public async Task<string> GetDownloadUrlAsync(NexusDownload download, CancellationToken token)
    {
        await semaphore.WaitAsync(token);
        try
        {
            logger?.LogTrace("Loading download page for file {download.FileName}.", download.FileName);
            var tcs = new TaskCompletionSource<string>();
            Handler.TaskCompletionSource = tcs;
            LoadUrl(download.Url);
            return await tcs.Task;
        }
        finally
        {
            Handler.TaskCompletionSource = null;
            semaphore.Release();
        }
    }

    /// <summary>
    /// Automatically click on slow download as soon as the page is loaded
    /// </summary>
    private void OnBrowserLoadEnd(object sender, LoadEndEventArgs e)
    {
        if (e.Frame.IsMain)
        {
            e.Frame.ExecuteJavaScript("""
                ;(function() {
                    function isNexusFilePage() {
                        const url = new URL(location.href);
                        return url.hostname.endsWith('nexusmods.com') &&
                               url.searchParams.has('file_id');
                    }

                	function clickButton() {
                		const btn = document.getElementById('slowDownloadButton')
                				   || document.querySelector('a[data-download-url], button[data-download-url]');
                		btn.click();
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
                """,
                e.Frame.Url, 0);
        }
    }

    public new void Dispose()
    {
        semaphore.Dispose();
        base.Dispose();
    }
}
