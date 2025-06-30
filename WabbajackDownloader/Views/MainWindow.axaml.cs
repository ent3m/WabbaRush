using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Reactive;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WabbajackDownloader.Core;

namespace WabbajackDownloader.Views;

public partial class MainWindow : Window
{
    private static readonly Uri repoUri = new("https://github.com/ent3m/WabbajackDownloader");
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
        Title = "Select folder to store downloaded mods",
        AllowMultiple = false
    };
    private const int minDownloadSize = 1;
    private const int maxDownloadSize = 9999;
    private const int minConcurrentDownload = 1;
    private const int maxConcurrentDownload = 10;
    private const int minRetry = 0;
    private const int maxRetry = 10;

    private readonly AppSettings settings;

    private List<NexusDownload>? downloads;
    private CookieContainer? container;
    private IStorageFolder? downloadFolder;
    private readonly CancellationTokenSource downloadTokenSource = new();

    private int currentPos;
    private int retryCount;
    private const int maxRetryCount = 3;

    public MainWindow()
    {
        InitializeComponent();

        settings = App.Settings;
        ToolTip.SetTip(bannerButton, repoUri);
        maxSizeBox.Minimum = minDownloadSize;
        maxSizeBox.Maximum = maxDownloadSize;
        maxDownloadBox.Minimum = minConcurrentDownload;
        maxDownloadBox.Maximum = maxConcurrentDownload;
        maxRetryBox.Minimum = minRetry;
        maxRetryBox.Maximum = maxRetry;
    }

    protected override async void OnOpened(EventArgs e)
    {
        // load settings
        var folderPath = settings.DownloadFolder;
        var folder = await StorageProvider.TryGetFolderFromPathAsync(folderPath);
        if (folder != null)
        {
            downloadFolder = folder;
            folderText.Text = folderPath;
        }
        else
        {
            folderText.Text = selectFolderMessage;
        }

        var filePath = settings.WabbajackFile;
        var file = await StorageProvider.TryGetFileFromPathAsync(filePath);
        if (file != null)
        {
            fileText.Text = filePath;
            await ExtractDownloads(file);
        }
        else
        {
            fileText.Text = selectFileMessage;
        }

        maxSizeBox.Value = Math.Clamp(settings.MaxDownloadSize, minDownloadSize, maxDownloadSize);
        maxDownloadBox.Value = Math.Clamp(settings.MaxConcurrentDownload, minConcurrentDownload, maxConcurrentDownload);
        maxRetryBox.Value = Math.Clamp(settings.MaxRetry, minRetry, maxRetry);

        // wire up observables
        var maxDownloadSizeValue = maxSizeBox.GetObservable(Slider.ValueProperty);
        maxDownloadSizeValue.Subscribe(new AnonymousObserver<double>(value => settings.MaxDownloadSize = double.ConvertToInteger<int>(value)));

        var maxConcurrentDownloadValue = maxDownloadBox.GetObservable(NumericUpDown.ValueProperty);
        maxConcurrentDownloadValue.Subscribe(new AnonymousObserver<decimal?>(value =>
        {
            if (value.HasValue)
                settings.MaxConcurrentDownload = decimal.ConvertToInteger<int>(value.Value);
        }));

        var maxRetryValue = maxRetryBox.GetObservable(NumericUpDown.ValueProperty);
        maxRetryValue.Subscribe(new AnonymousObserver<decimal?>(value =>
        {
            if (value.HasValue)
                settings.MaxRetry = decimal.ConvertToInteger<int>(value.Value);
        }));

        base.OnOpened(e);
    }

    /// <summary>
    /// Open source repository using system's default browser
    /// </summary>
    private void OpenRepo(object sender, RoutedEventArgs args) => Launcher.LaunchUriAsync(repoUri);

    /// <summary>
    /// Let the user choose the wabbajack file and scan it for downloadable mods
    /// </summary>
    private async void OpenWabbajackFilePicker(object sender, RoutedEventArgs args)
    {
        var file = await StorageProvider.OpenFilePickerAsync(wabbajackFilePickerOptions);
        if (file.Count == 0) return;
        var storageFile = file[0];
        var filePath = storageFile.TryGetLocalPath() ?? storageFile.Path.ToString();
        fileText.Text = settings.WabbajackFile = filePath;
        await ExtractDownloads(storageFile);
        EnableDownloadButton();
    }

    private async Task ExtractDownloads(IStorageFile storageFile)
    {
        try
        {
            using var stream = await storageFile.OpenReadAsync();
            downloads = ModlistExtractor.ExtractDownloadLinks(stream);

            if (downloads != null)
            {
                downloadProgressBar.Maximum = downloads.Count;
                downloadProgressBar.IsVisible = true;
            }
            else
            {
                downloadProgressBar.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            App.Logger.LogCritical(ex.GetBaseException(), "Unable to extract mods from wabbajack file {settings.WabbajackFile}.", settings.WabbajackFile);
        }
    }

    /// <summary>
    /// Let the user choose download folder
    /// </summary>
    private async void OpenDownloadFolderPicker(object sender, RoutedEventArgs args)
    {
        var folder = await StorageProvider.OpenFolderPickerAsync(downloadFolderPickerOptions);
        if (folder.Count == 0) return;
        downloadFolder = folder[0];
        var folderPath = downloadFolder.TryGetLocalPath() ?? downloadFolder.Path.ToString();
        folderText.Text = settings.DownloadFolder = folderPath;
        EnableDownloadButton();
    }

    /// <summary>
    /// Display a nexus signin window and attempt to retrieve user and session cookies
    /// </summary>
    private async void DisplaySigninWindow(object sender, RoutedEventArgs args)
    {
        using var signinWindow = new NexusSigninWindow();
        // set this so that any popup can attach to signin window's lifespan
        // if we don't then it will be attached to main window's lifespan instead
        App.SigninWindow = signinWindow;
        container = await signinWindow.ShowAndGetCookiesAsync(this);
        // get rid of the reference to signin window so that it can be disposed
        App.SigninWindow = null;

        App.Logger.LogInformation("Cookies added: {count}.", container.Count);
        App.Logger.LogInformation("Cookie header: {header}", container.GetCookieHeader(new Uri("https://www.nexusmods.com/Core/Libs/Common/Managers/Downloads?GenerateDownloadUrl")));
        
        EnableDownloadButton();
    }

    /// <summary>
    /// Check if conditions are satisfied to begin download. If not then disable it
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
        var downloader = new NexusDownloader(downloadFolder, downloads, container);
        downloader.Downloading += UpdateDownloadProgress;

        // disable controls
        folderPickerButton.IsEnabled = false;
        filePickerButton.IsEnabled = false;
        loginButton.IsEnabled = false;
        optionsBox.IsExpanded = false;
        optionsBox.IsEnabled = false;
        downloadButton.IsEnabled = false;

        // prepare variables
        currentPos = 0;
        retryCount = 0;
        try
        {
            await downloader.DownloadFilesAsync(0, downloadTokenSource.Token);
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
            optionsBox.IsEnabled = true;
            downloadButton.IsEnabled = true;
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
            if (retryCount < maxRetryCount)
            {
                // we're stuck in the same position, so we increase retry count
                if (downloader.Position == currentPos)
                    retryCount++;
                // we moved on to a new position since the last retry. reset retry count
                else
                    retryCount = 0;

                App.Logger.LogCritical(ex.GetBaseException(), "Download failed. Attempting again for the {retryCount}th time.", retryCount);
                // attempt to download again from the failed position
                currentPos = downloader.Position;
                await DownloadFilesCore(downloader, currentPos);
            }
            else
            {
                App.Logger.LogError(ex.GetBaseException(), "Download failed. No retry remaining.");
                throw;
            }
        }
    }

    private void UpdateDownloadProgress(int position, string fileName, ulong size)
    {
        downloadProgressBar.Value = position + 1;
        (var value, var suffix) = FileSizeFormatter(size);
        infoText.Text = $"{fileName} ({value} {suffix})";
    }

    private readonly static string[] sizeSuffixes = ["KB", "MB", "GB", "TB"];
    private static (string, string) FileSizeFormatter(ulong bytes)
    {
        if (bytes < 0)
            return ("NaN", string.Empty);

        if (bytes < 1024)
            return ($"{bytes}", "B");

        double size = bytes;
        int order = -1;
        do
        {
            size /= 1024;
            order++;
        }
        while (size > 1024 && order < sizeSuffixes.Length - 1);

        return ($"{size:0.#}", $"{sizeSuffixes[order]}");
    }

    // Cancel ongoing downloads before closing
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        downloadTokenSource.Cancel();
        base.OnClosing(e);
    }
}