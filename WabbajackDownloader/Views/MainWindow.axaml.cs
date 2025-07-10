using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Reactive;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WabbajackDownloader.Configuration;
using WabbajackDownloader.Core;
using WabbajackDownloader.Exceptions;

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
    private readonly ILoggerProvider? loggerProvider;
    private readonly ILogger? logger;

    private CookieContainer? container;
    private readonly CancellationTokenSource downloadTokenSource = new();

#if DEBUG
    // parameterless constructor for xaml previewer
    public MainWindow() : this(AppSettings.GetDefaultSettings(), null)
    {

    }
#endif

    public MainWindow(AppSettings settings, ILoggerProvider? loggerProvider)
    {
        InitializeComponent();

        this.settings = settings;
        this.loggerProvider = loggerProvider;
        logger = loggerProvider?.CreateLogger(nameof(MainWindow));
        ToolTip.SetTip(bannerButton, repoUri);
        maxSizeBox.Minimum = minDownloadSize;
        maxSizeBox.Maximum = maxDownloadSize;
        maxDownloadBox.Minimum = minConcurrentDownload;
        maxDownloadBox.Maximum = maxConcurrentDownload;
        maxRetryBox.Minimum = minRetry;
        maxRetryBox.Maximum = maxRetry;
    }

    protected override void OnOpened(EventArgs e)
    {
        // load settings
        var folderPath = settings.DownloadFolder;
        if (Directory.Exists(folderPath))
            folderText.Text = folderPath;
        else
            folderText.Text = selectFolderMessage;

        var filePath = settings.WabbajackFile;
        if (File.Exists(filePath))
            fileText.Text = filePath;
        else
            fileText.Text = selectFileMessage;

        maxSizeBox.Value = Math.Clamp(settings.MaxDownloadSize, minDownloadSize, maxDownloadSize);
        maxDownloadBox.Value = Math.Clamp(settings.MaxConcurrentDownload, minConcurrentDownload, maxConcurrentDownload);
        maxRetryBox.Value = Math.Clamp(settings.MaxRetries, minRetry, maxRetry);

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
                settings.MaxRetries = decimal.ConvertToInteger<int>(value.Value);
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
    }

    /// <summary>
    /// Let the user choose a download folder
    /// </summary>
    private async void OpenDownloadFolderPicker(object sender, RoutedEventArgs args)
    {
        var folder = await StorageProvider.OpenFolderPickerAsync(downloadFolderPickerOptions);
        if (folder.Count == 0) return;

        var downloadFolder = folder[0];
        var folderPath = downloadFolder.TryGetLocalPath() ?? downloadFolder.Path.ToString();
        folderText.Text = settings.DownloadFolder = folderPath;
    }

    /// <summary>
    /// Display a nexus signin window and attempt to retrieve session cookies
    /// </summary>
    private async void DisplaySigninWindow(object sender, RoutedEventArgs args)
    {
        var signinWindow = new NexusSigninWindow(settings.NexusLandingPage, loggerProvider?.CreateLogger(nameof(NexusSigninWindow)));
        container = await signinWindow.ShowAndGetCookiesAsync(this);
    }

    /// <summary>
    /// Begin downloading mods
    /// </summary>
    private async void DownloadFiles(object sender, RoutedEventArgs args)
    {
        settings.SaveSettings();

        // ready check
        if (settings.DownloadFolder == null)
        {
            infoText.Text = "Please select a download folder.";
            folderText.Text = selectFolderMessage;
            return;
        }
        if (settings.WabbajackFile == null)
        {
            infoText.Text = "Please select a wabbajack file.";
            fileText.Text = selectFileMessage;
            return;
        }
        if (container == null)
        {
            infoText.Text = "Please login to your nexus account.";
            return;
        }

        DisableControls();

        // extract mod list
        List<NexusDownload> downloads;
        try
        {
            infoText.Text = "Extracting mod list...";
            await Task.Delay(10);   // pause for the UI to update
            downloads = ExtractDownloads(settings.WabbajackFile);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unable to extract download links from wabbajack file.");
            infoText.Text = "Unable to extract download links from wabbajack file. Check log for more info.";
            settings.WabbajackFile = null;
            fileText.Text = selectFileMessage;
            RestoreControls();
            return;
        }

        // prepare downloader
        var downloader = new NexusDownloader(settings.DownloadFolder, downloads, container,
            settings.MaxDownloadSize, settings.BufferSize, settings.MaxRetries,
            settings.MinRetryDelay, settings.MaxRetryDelay, settings.CheckHash,
            settings.MaxConcurrentDownload, settings.UserAgent,
            settings.DiscoverExistingFiles,
            loggerProvider?.CreateLogger(nameof(NexusDownloader)),
            new Progress<int>(UpdateDownloadProgress),
            new ProgressPool(progressContainer));

        progressContainer.IsVisible = true;
        downloadProgressBar.Maximum = downloads.Count;

        // begin download
        try
        {
            infoText.Text = "Downloading...";
            await downloader.DownloadAsync(downloadTokenSource.Token);
            infoText.Text = "All done!";
        }
        catch (OperationCanceledException ex)
        {
            logger?.LogInformation(ex, "Download process has been canceled.");
        }
        catch (InvalidJsonResponseException ex)
        {
            logger?.LogError(ex, "Download process has stopped due to invalid or missing credentials.");
            infoText.Text = "Cannot download from Nexusmods due to invalid or missing credentials. Please login again.";
            container = null;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger?.LogError(ex, "Download process has stopped due to insufficient privileges.");
            infoText.Text = "Unable to access download folder due to insufficient privileges. Please select another folder.";
            settings.DownloadFolder = null;
            folderText.Text = selectFolderMessage;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Download process has stopped due to an exception.");
            var exceptionName = ex.GetType().Name;
            infoText.Text = $"Download has failed due to {exceptionName}. Check log for more info.";
        }
        finally
        {
            downloader.Dispose();
            RestoreControls();
        }
    }

    /// <summary>
    /// Extract downloadable mods from wabbajack file
    /// </summary>
    private List<NexusDownload> ExtractDownloads(string filePath)
    {
        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        var extractions = ModlistExtractor.ExtractDownloadLinks(stream, loggerProvider?.CreateLogger(nameof(ModlistExtractor)));
        if (extractions.Count == 0)
            throw new InvalidDataException("The selected wabbajack file does not contain any downloadable mods.");
        else
            return extractions;
    }

    private void DisableControls()
    {
        folderPickerButton.IsEnabled = false;
        filePickerButton.IsEnabled = false;
        loginButton.IsEnabled = false;
        optionsBox.IsEnabled = false;
        downloadButton.IsEnabled = false;
        optionsBox.IsExpanded = false;
    }

    private void RestoreControls()
    {
        folderPickerButton.IsEnabled = true;
        filePickerButton.IsEnabled = true;
        loginButton.IsEnabled = true;
        optionsBox.IsEnabled = true;
        downloadButton.IsEnabled = true;
        progressContainer.IsVisible = false;
    }

    private void UpdateDownloadProgress(int i)
    {
        downloadProgressBar.Value = i;
    }

    // Cancel ongoing downloads and save settings before closing
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        downloadTokenSource.Cancel();
        downloadTokenSource.Dispose();
        Xilium.CefGlue.CefRuntime.Shutdown();
        settings.SaveSettings();
        base.OnClosing(e);
    }
}