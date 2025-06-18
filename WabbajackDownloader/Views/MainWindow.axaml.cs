using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Reactive;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace WabbajackDownloader.Views;

public partial class MainWindow : Window
{
    private static readonly Uri repoUri = new("https://www.github.com");
    private const string selectFileMessage = "Select wabbajack file";
    private const string selectFolderMessage = "Select download folder";
    private static readonly FilePickerFileType wabbajackFileType = new("Wabbajack file")
    {
        Patterns = ["*.wabbajack"]
    };
    private static readonly FilePickerOpenOptions wabbajackFilePickerOptions = new()
    {
        Title = "Select wabbajack file for mod list extraction",
        AllowMultiple = false,
        FileTypeFilter = [wabbajackFileType]
    };
    private static readonly FolderPickerOpenOptions downloadFolderPickerOptions = new()
    {
        Title = "Select location to store downloaded mods",
        AllowMultiple = false
    };

    private List<NexusDownload>? downloads;
    private CookieContainer? container;
    private IStorageFolder? downloadFolder;
    private readonly CancellationTokenSource downloadTokenSource = new();
    private long maxDownloadSize;

    private bool autoRetry;
    private int currentPos;
    private int retryCount;
    private const int maxRetryCount = 3;

    public MainWindow()
    {
        InitializeComponent();

        ToolTip.SetTip(bannerButton, repoUri);
        fileText.Text = selectFileMessage;
        folderText.Text = selectFolderMessage;
        var maxDownloadValue = maxSizeBox.GetObservable(NumericUpDown.ValueProperty);
        maxDownloadValue.Subscribe(new AnonymousObserver<decimal?>(value => maxDownloadSize = decimal.ToInt64(value!.Value)));
        var retryValue = autoRetryBox.GetObservable(CheckBox.IsCheckedProperty);
        retryValue.Subscribe(new AnonymousObserver<bool?>(value => autoRetry = value ?? false));
    }

    /// <summary>
    /// Open source repository using the default browser
    /// </summary>
    private void OpenRepo(object sender, RoutedEventArgs args)
        => Launcher.LaunchUriAsync(repoUri);

    /// <summary>
    /// Let the user choose the wabbajack file and scan it for downloadable mods
    /// </summary>
    private async void OpenWabbajackFilePicker(object sender, RoutedEventArgs args)
    {
        var file = await StorageProvider.OpenFilePickerAsync(wabbajackFilePickerOptions);
        if (file.Count == 0) return;
        var storageFile = file[0];
        var filePath = storageFile.TryGetLocalPath() ?? storageFile.Path.ToString();
        fileText.Text = filePath;
        try
        {
            using var stream = await storageFile.OpenReadAsync();
            downloads = ModlistExtractor.ExtractDownloadLinks(stream);
        }
        catch (Exception ex)
        {
#if DEBUG
            Debug.WriteLine("Unable to extract mods from wabbajack file.");
            Debug.WriteLine($"{ex.GetBaseException()}: {ex.Message}");
#endif
        }
        if (downloads != null && downloads.Count > 0)
        {
            downloadProgressBar.Maximum = downloads.Count;
            downloadProgressBar.IsVisible = true;
        }
        EnableDownloadButton();
    }

    /// <summary>
    /// Let the user choose download folder and scan it for existing downloaded files
    /// </summary>
    private async void OpenDownloadFolderPicker(object sender, RoutedEventArgs args)
    {
        var folder = await StorageProvider.OpenFolderPickerAsync(downloadFolderPickerOptions);
        if (folder.Count == 0) return;
        downloadFolder = folder[0];
        var folderPath = downloadFolder.TryGetLocalPath() ?? downloadFolder.Path.ToString();
        folderText.Text = folderPath;

        EnableDownloadButton();
    }

    /// <summary>
    /// Display a nexus signin window and attempt to retrieve user and session cookies
    /// </summary>
    private async void DisplaySigninWindow(object sender, RoutedEventArgs args)
    {
        var signinWindow = new NexusSigninWindow();
        App.SigninWindow = signinWindow;
        container = await signinWindow.ShowAndGetCookiesAsync(this);
        App.SigninWindow = null;
#if DEBUG
        Debug.WriteLine($"Cookies added: {container.GetCookies(new Uri("https://www.nexusmods.com")).Count}");
        Debug.WriteLine($"Cookie header: {container.GetCookieHeader(new Uri("https://www.nexusmods.com/Core/Libs/Common/Managers/Downloads?GenerateDownloadUrl"))}");
#endif
        EnableDownloadButton();
    }

    /// <summary>
    /// Check if download button can be enabled. If not then disable it
    /// </summary>
    private bool EnableDownloadButton() => downloadButton.IsEnabled = downloads != null && container != null && downloadFolder != null;

    /// <summary>
    /// Begin downloading mods
    /// </summary>
    private async void DownloadFiles(object sender, RoutedEventArgs args)
    {
        // ready check
        downloadDoneIcon.IsVisible = false;

        if (container == null || container.Count == 0)
        {
            infoText.Text = "Unable to acquire user credentials. Please login to your nexus account.";
            return;
        }
        if (downloadFolder == null)
        {
            infoText.Text = "Unable to locate download folder. Please enter a valid path.";
            folderText.Text = selectFolderMessage;
            return;
        }
        if (downloads == null || downloads.Count == 0)
        {
            infoText.Text = "Unable to find any downloadable mods from the selected wabbajack file.";
            fileText.Text = selectFileMessage;
            return;
        }

        // prepare downloader
        var downloader = new NexusDownloader(downloadFolder, downloads, container, (ulong)maxDownloadSize * 1024 * 1024);
        downloader.Downloading += UpdateDownloadProgress;

        // disable controls
        folderPickerButton.IsEnabled = false;
        filePickerButton.IsEnabled = false;
        loginButton.IsEnabled = false;
        maxSizeBox.IsEnabled = false;
        downloadButton.IsEnabled = false;

        // prepare variables
        currentPos = 0;
        retryCount = 0;
        try
        {
            await downloader.InitializeAsync();
            await DownloadFilesCore(downloader, 0);
            infoText.Text = "All done!";
            downloadDoneIcon.IsVisible = true;
        }
        catch (TaskCanceledException)
        {
            infoText.Text = "Download is cancelled by the user.";
        }
        catch (InvalidOperationException)
        {
            infoText.Text = $"Try logging in again.\n{typeof(InvalidOperationException)}";
            container = null;
        }
        catch (UnauthorizedAccessException)
        {
            infoText.Text = $"Select another download folder.\n{typeof(UnauthorizedAccessException)}";
            downloadFolder = null;
            folderText.Text = selectFolderMessage;
        }
        catch (Exception ex)
        {
            infoText.Text = $"{ex.GetBaseException()}";
        }
        finally
        {
            downloader.Dispose();
            //restore controls
            folderPickerButton.IsEnabled = true;
            filePickerButton.IsEnabled = true;
            loginButton.IsEnabled = true;
            maxSizeBox.IsEnabled = true;
            EnableDownloadButton();
        }
    }

    /// <summary>
    /// Run on a recursive loop if autoRetry is true. Max recursive layer = maxRetryCount
    /// </summary>
    private async Task DownloadFilesCore(NexusDownloader downloader, int position)
    {
        try
        {
            await downloader.DownloadFilesAsync(position, downloadTokenSource.Token);
        }
        catch (Exception ex) when (ex is TaskCanceledException or InvalidOperationException or UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (autoRetry && retryCount < maxRetryCount)
            {
                // we're stuck in the same position, so we increase retry count
                if (downloader.Position == currentPos)
                    retryCount++;
                // we moved on to a new position since the last retry. reset retry count
                else
                    retryCount = 0;
#if DEBUG
                Debug.WriteLine($"Download failed. Attempting again for the {retryCount} time.\n{ex.GetBaseException()}: {ex.Message}");
#endif
                // attempt to download again from the failed position
                currentPos = downloader.Position;
                await DownloadFilesCore(downloader, currentPos);
            }
            else
                throw;
        }
    }

    private void UpdateDownloadProgress(int position, string fileName, ulong size)
    {
        downloadProgressBar.Value = position + 1;
        infoText.Text = $"{fileName} ({FileSizeFormatter(size)})";
    }

    private readonly static string[] sizeSuffixes = ["KB", "MB", "GB", "TB"];
    private static string FileSizeFormatter(ulong bytes)
    {
        if (bytes < 0)
            return "NaN";

        if (bytes < 1024)
            return $"{bytes} B";

        double size = bytes;
        int order = -1;
        do
        {
            size /= 1024;
            order++;
        }
        while (size > 1024 && order < sizeSuffixes.Length - 1);

        return $"{size:0.#}{sizeSuffixes[order]}";
    }

    // Cancel ongoing downloads before closing
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        downloadTokenSource.Cancel();
        base.OnClosing(e);
    }
}