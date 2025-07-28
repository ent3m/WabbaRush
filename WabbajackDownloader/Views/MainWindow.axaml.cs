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
using WabbajackDownloader.Common;
using WabbajackDownloader.Configuration;
using WabbajackDownloader.Core;
using WabbajackDownloader.Exceptions;
using WabbajackDownloader.Extensions;
using WabbajackDownloader.ModList;

namespace WabbajackDownloader.Views;

public partial class MainWindow : Window
{
    private static readonly Uri repoUri = new("https://github.com/ent3m/WabbaRush");
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
    private const int downloadSizeLowerBound = 1;
    private const int downloadSizeUpperBound = 9999;
    private const int concurrencyLowerBound = 1;
    private const int concurrencyUpperBound = 10;
    private const int retriesLowerBound = 0;
    private const int retriesUpperBound = 10;

    private readonly AppSettings settings;
    private readonly ILoggerProvider? loggerProvider;
    private readonly ILogger? logger;

    private CookieContainer? container;
    private readonly CancellationTokenSource downloadTokenSource = new();

#if DEBUG
    // parameterless constructor for xaml previewer
    public MainWindow() : this(null, new(), null)
    {

    }
#endif

    public MainWindow(ModListMetadata[]? modlists, AppSettings settings, ILoggerProvider? loggerProvider)
    {
        InitializeComponent();

        this.settings = settings;
        this.loggerProvider = loggerProvider;
        logger = loggerProvider?.CreateLogger(nameof(MainWindow));

        // setup controls
        ToolTip.SetTip(bannerButton, repoUri);
        maxSizeBox.Minimum = downloadSizeLowerBound;
        maxSizeBox.Maximum = downloadSizeUpperBound;
        maxDownloadBox.Minimum = concurrencyLowerBound;
        maxDownloadBox.Maximum = concurrencyUpperBound;
        maxRetryBox.Minimum = retriesLowerBound;
        maxRetryBox.Maximum = retriesUpperBound;
        modlistBox.ItemsSource = modlists;

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

        if (modlists != null && settings.SelectedModList != null)
            modlistBox.SelectedIndex = Array.FindIndex(modlists, t => t.Title == settings.SelectedModList);

        maxSizeBox.Value = Math.Clamp(settings.MaxDownloadSize, downloadSizeLowerBound, downloadSizeUpperBound);
        maxDownloadBox.Value = Math.Clamp(settings.MaxConcurrency, concurrencyLowerBound, concurrencyUpperBound);
        maxRetryBox.Value = Math.Clamp(settings.MaxRetries, retriesLowerBound, retriesUpperBound);
        useLocalFileBox.IsChecked = settings.UseLocalFile;

        // wire up observables
        var maxDownloadSizeValue = maxSizeBox.GetObservable(Slider.ValueProperty);
        maxDownloadSizeValue.Subscribe(new AnonymousObserver<double>(value => settings.MaxDownloadSize = double.ConvertToInteger<int>(value)));

        var maxConcurrentDownloadValue = maxDownloadBox.GetObservable(NumericUpDown.ValueProperty);
        maxConcurrentDownloadValue.Subscribe(new AnonymousObserver<decimal?>(value =>
        {
            if (value.HasValue)
                settings.MaxConcurrency = decimal.ConvertToInteger<int>(value.Value);
        }));

        var maxRetryValue = maxRetryBox.GetObservable(NumericUpDown.ValueProperty);
        maxRetryValue.Subscribe(new AnonymousObserver<decimal?>(value =>
        {
            if (value.HasValue)
                settings.MaxRetries = decimal.ConvertToInteger<int>(value.Value);
        }));

        var useLocalValue = useLocalFileBox.GetObservable(CheckBox.IsCheckedProperty);
        useLocalValue.Subscribe(new AnonymousObserver<bool?>(value =>
        {
            if (value.HasValue)
                settings.UseLocalFile = value.Value;
        }));

        var selectedModListValue = modlistBox.GetObservable(ComboBox.SelectedItemProperty);
        selectedModListValue.Subscribe(new AnonymousObserver<object?>(item =>
        {
            if (item is ModListMetadata metadata)
                settings.SelectedModList = metadata.Title;
        }));
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
        if (settings.UseLocalFile)
        {
            if (settings.WabbajackFile == null)
            {
                infoText.Text = "Please select a wabbajack file.";
                return;
            }
        }
        else
        {
            if (modlistBox.SelectedItem == null)
            {
                infoText.Text = "Please select a mod list.";
                return;
            }
        }
        if (settings.DownloadFolder == null)
        {
            infoText.Text = "Please select a download folder.";
            return;
        }
        if (container == null)
        {
            infoText.Text = "Please login to your nexus account.";
            return;
        }

        logger?.LogInformation("Starting download process.");
        DisableControls();
        var circuitBreaker = new CircuitBreaker(settings.MaxRetries, settings.RetryDelay, settings.DelayMultiplier, settings.DelayJitter);

        // extract mod list
        List<NexusDownload> downloads;
        try
        {
            string file;
            if (settings.UseLocalFile)
            {
                file = settings.WabbajackFile!;
                logger?.LogTrace("Using local wabbajack file {file}.", file);
            }
            else
            {
                var modlistDownloader = new ModListDownloader(settings, circuitBreaker, loggerProvider?.CreateLogger(nameof(ModListDownloader)));
                var metadata = (ModListMetadata)modlistBox.SelectedItem!;
                var progressLock = new Lock();
                var progress = new Progress<long>(i =>
                {
                    lock (progressLock)
                    {
                        downloadProgressBar.Value += i;
                    }
                });

                infoText.Text = "Downloading wabbajack file...";
                progressContainer.IsVisible = true;
                downloadProgressBar.Maximum = metadata.DownloadMetadata.Size;
                downloadProgressBar.Value = 0;
                downloadProgressBar.ProgressTextFormat = metadata.DownloadMetadata.Size.DisplayByteSize() + " ({1:0}%)";

                logger?.LogTrace("Getting wabbajack file for {title} from Wabbajack CDN.", metadata.Title);
                file = await modlistDownloader.DownloadWabbajackAsync(metadata, progress, downloadTokenSource.Token);
            }

            infoText.Text = "Extracting download links...";
            await Task.Delay(10);   // pause for the UI to update
            logger?.LogTrace("Extracting mods from wabbajack file.");
            downloads = ExtractDownloads(file);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unable to extract download links from wabbajack file.");
            infoText.Text = "Unable to extract download links from wabbajack file. Check log for more info.";
            settings.WabbajackFile = null;
            fileText.Text = selectFileMessage;

            progressContainer.IsVisible = false;
            RestoreControls();
            return;
        }

        // prepare downloader
        var downloader = new NexusDownloader(settings.DownloadFolder, downloads, container,
            settings, loggerProvider?.CreateLogger(nameof(NexusDownloader)),
            new DownloadProgressPool(progressContainer),
            circuitBreaker);

        // begin downloading mods
        try
        {
            infoText.Text = "Downloading...";
            progressContainer.IsVisible = true;
            downloadProgressBar.Maximum = downloads.Count;
            downloadProgressBar.Value = 0;
            downloadProgressBar.ProgressTextFormat = "{0}/{3} ({1:0}%)";

            logger?.LogTrace("Extraction completed. Downloading individual mods.");
            await downloader.DownloadAsync(new Progress<int>(i => downloadProgressBar.Value = i), downloadTokenSource.Token);

            infoText.Text = "All done!";
            logger?.LogInformation("Download process completed.");
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
            progressContainer.IsVisible = false;
            RestoreControls();
        }
    }

    /// <summary>
    /// Extract downloadable mods from wabbajack file
    /// </summary>
    private List<NexusDownload> ExtractDownloads(string filePath)
    {
        var extractions = ModlistExtractor.ExtractDownloadLinks(filePath, loggerProvider?.CreateLogger(nameof(ModlistExtractor)));
        if (extractions.Count == 0)
            throw new InvalidDataException("The selected wabbajack file does not contain any downloadable mods.");
        else
            return extractions;
    }

    private void DisableControls()
    {
        folderPickerButton.IsEnabled = false;
        filePickerButton.IsEnabled = false;
        modlistBox.IsEnabled = false;
        loginButton.IsEnabled = false;
        optionsBox.IsEnabled = false;
        downloadButton.IsEnabled = false;
        optionsBox.IsExpanded = false;
    }

    private void RestoreControls()
    {
        folderPickerButton.IsEnabled = true;
        filePickerButton.IsEnabled = true;
        modlistBox.IsEnabled = true;
        loginButton.IsEnabled = true;
        optionsBox.IsEnabled = true;
        downloadButton.IsEnabled = true;
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
