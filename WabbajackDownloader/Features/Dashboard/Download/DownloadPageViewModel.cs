using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using WabbajackDownloader.Common.Dialogs;
using WabbajackDownloader.Common.Hashing;
using WabbajackDownloader.Common.Progress;
using WabbajackDownloader.Common.Retry;
using WabbajackDownloader.Features.Frame;
using WabbajackDownloader.Features.WabbajackModList;
using WabbajackDownloader.Features.WebView;
using Waypoint;

namespace WabbajackDownloader.Features.Dashboard;

internal sealed partial class DownloadPageViewModel : ObservableObject, INavigable
{
    private readonly INavigator _navigator;
    private readonly ModListDownloader _modListDownloader;
    private readonly ModListExtractor _modListExtractor;
    private readonly AppSettings _settings;
    private readonly ILogger<DownloadPageViewModel> _logger;
    private readonly long _fileSizeLimit;
    private ModListPreparationOptions? _preparationOptions;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddressText))]
    public partial Uri Address { get; set; } = new Uri(NexusLoginUrl + AccountSecurityUrl);
    public string AddressText => Address.ToString();

    [ObservableProperty]
    public partial IJavaScriptRunner JSRunner { get; set; }

    [ObservableProperty]
    public partial bool BlockInput { get; set; } = false;

    [ObservableProperty]
    public partial string? DownloadFolderPath { get; set; }

    public ObservableCollection<DownloadProgressDisplay> DownloadProgresses { get; private set; } = [];

    [ObservableProperty]
    public partial ProgressDisplay<long>? TotalProgress { get; set; }

    public DownloadPageViewModel(AppSettings settings, ILogger<DownloadPageViewModel> logger,
        IJavaScriptRunner jsRunner, INavigator navigator,
        ModListDownloader modListDownloader, ModListExtractor modListExtractor)
    {
        _navigator = navigator;
        _modListDownloader = modListDownloader;
        _modListExtractor = modListExtractor;
        _settings = settings;
        _logger = logger;
        JSRunner = jsRunner;
        jsRunner.WebMessageReceived += OnWebMessageReceived;
        DownloadFolderPath = settings.DownloadFolder;
        _fileSizeLimit = settings.FileSizeUnit.ToBytes(settings.MaxDownloadSize);
    }

    Task INavigable.OnNavigatingToAsync(object? parameter, CancellationToken cancellationToken)
    {
        if (parameter is ModListPreparationOptions preparationOptions)
            _preparationOptions = preparationOptions;

        return Task.CompletedTask;
    }

    Task<bool> INavigable.OnNavigatingFromAsync(CancellationToken cancellationToken)
    {
        _downloadRequestLimiter.Dispose();
        return Task.FromResult(true);
    }

    [RelayCommand]
    private void CancelDownloads()
    {
        DownloadCommand.Cancel();
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        // CancelDownloads must be triggered by the user before navigating back, so it's safe to navigate without canceling
        await _navigator.NavigateAsync<SetupPage, Shell>();
    }

    #region NexusDownload
    private const string NexusLoginUrl = "https://users.nexusmods.com/auth/sign_in?redirect_url=";
    private const string AccountSecurityUrl = "https://users.nexusmods.com/account/security";
    private const string AutoDownloadScript = """
                !function(){function t(t){window.chrome&&window.chrome.webview&&window.chrome.webview.postMessage(t)}if(!function(){const t=new URL(location.href);return t.hostname.endsWith(".nexusmods.com")&&t.searchParams.has("file_id")}())return t("NOT_FILE_PAGE");if(!function(){const t=document.querySelector("quick-search, mobile-quick-search");if(t&&"true"===t.getAttribute("is-logged-in"))return!0;const o=document.querySelector("[user-is-logged-in]");return o?"true"===o.getAttribute("user-is-logged-in"):!document.body.classList.contains("logged-out")}())return t("AUTH_REQUIRED");let o=0,e=0,n=0;function r(t){let o=Math.max(1,t);return 200*o*o}function c(t){const o=t.querySelectorAll('button, a, [role="button"]');for(const t of o)if("slow download"===t.textContent.trim().toLowerCase())return t;for(const t of o){if("BUTTON"!==t.tagName)continue;const o=t.className.toLowerCase();if(o.includes("nxm-button")&&o.includes("secondary"))return t}return null}function i(t){const o=t.querySelectorAll('button, a, [role="button"]');for(const t of o)if("standard download"===t.textContent.trim().toLowerCase())return t;for(const t of o){if("BUTTON"!==t.tagName)continue;const o=t.className.toLowerCase();if(o.includes("nxm-button")&&o.includes("secondary")&&!o.includes("weak")&&!o.includes("strong"))return t}return null}function s(t,o){const e=o(t);if(e)return e;const n=t.querySelectorAll("*");for(const t of n)if(t.shadowRoot){const e=s(t.shadowRoot,o);if(e)return e}return null}function u(){const o=function(){const t=document.querySelector("mod-file-download");if(t&&t.shadowRoot){const o=c(t.shadowRoot);if(o)return o}return s(document,c)}();if(!o)return e<2?(e++,void setTimeout(u,r(e))):void t("BUTTON_NOT_FOUND");o.click();const a=setInterval(()=>{n++;const t=function(){const t=document.getElementById("next-shadow-root");if(t&&t.shadowRoot){const o=i(t.shadowRoot);if(o)return o}return s(document,i)}();t?(clearInterval(a),t.click()):n>=10&&clearInterval(a)},50)}!async function e(){const n=document.querySelector("mod-file-download"),c=n?.getAttribute("file-id"),i=n?.getAttribute("game-id");if(n&&c&&i)try{const o=await fetch("/Core/Libs/Common/Managers/Downloads?GenerateDownloadUrl",{method:"POST",credentials:"include",headers:{"Content-Type":"application/x-www-form-urlencoded"},body:new URLSearchParams({game_id:i,fid:c,collection_id:"0"}).toString()});if(401===o.status)return t("AUTH_REQUIRED");if(429===o.status)return t("TOO_MANY_REQUESTS");if(!o.ok)return u();const e=await o.json();e&&e.url?window.location.href=e.url:u()}catch{u()}else o<3?(o++,setTimeout(e,r(o))):u()}()}();
                """;

    private bool _loginChecked = false;
    private bool _loggedIn = false;
    private long _downloadCount = 0;
    private readonly SemaphoreSlim _downloadRequestLimiter = new(1, 1);
    private DownloadMonitor _monitor = DownloadMonitor.None;

    /* Why `volatile` and `Interlocked` are not needed:
     * Writes to this field occur on background threads; reads occur on the UI thread during WebView event callbacks.
     * Immediately after writing to this field, Address is updated. This marshals a property-changed notification across the
     * thread boundary to the UI layer, establishing a full memory barrier, guaranteeing visibility to all subsequent reads to this field.
     * Similarly, writes to this field occur immediately before SemaphoreSlim is released, which also establishes a full memory barrier. */
    private DownloadRequest? _currentRequest;

    /// <summary>
    /// Resolve download links from ModListPreparationOptions and return a dictionary of DownloadContext
    /// </summary>
    private async Task<FrozenDictionary<Hash, DownloadContext>> ResolveModList(CancellationToken cancellationToken)
    {
        // Extract if local
        if (_preparationOptions is LocalModListPreparationOptions local)
        {
            return _modListExtractor
                .ExtractDownloadLinks(local.LocalFilePath)
                .ToFrozenDictionary(static d => d.Hash, static d => new DownloadContext(d));
        }
        // Download and extract if remote
        if (_preparationOptions is RemoteModListPreparationOptions remote)
        {
            var metadata = remote.Metadata;
            TotalProgress = metadata.DownloadMetadata is null ?
                new ProgressDisplay<long>(title: metadata.Title) :
                new DownloadProgressDisplay(metadata.DownloadMetadata.Size, metadata.Title);

            return _modListExtractor
                .ExtractDownloadLinks(await _modListDownloader
                .DownloadModListAsync(remote.Metadata, TotalProgress, cancellationToken))
                .ToFrozenDictionary(static d => d.Hash, static d => new DownloadContext(d));
        }
        throw new InvalidOperationException($"ModList preparation options are null or unsupported: {_preparationOptions?.GetType().Name ?? "null"}");
    }

    /// <summary>
    /// Partitions an <see cref="ImmutableArray{T}"/> into segments and return the requested segment.
    /// </summary>
    /// <param name="input">The array to partition.</param>
    /// <param name="part">The 1-based index of the specific segment to retrieve.</param>
    /// <param name="totalParts">The total number of segments to divide the array into.</param>
    private static ReadOnlyMemory<T> GetPart<T>(ImmutableArray<T> input, int part, int totalParts)
    {
        if (input.IsDefaultOrEmpty)
            return default;

        totalParts = Math.Max(1, totalParts);
        part = Math.Clamp(part, 1, totalParts);

        long length = input.Length; // Use long to avoid integer overflow
        int start = (int)(length * (part - 1) / totalParts);
        int end = (int)(length * part / totalParts);

        return input.AsMemory()[start..end];
    }

    [RelayCommand(CanExecute = nameof(CanExecuteDownloadCommand))]
    private async Task DownloadAsync(CancellationToken cancellationToken)
    {
        // Warn the user if they aren't logged in, but still allow them to proceed if they choose to.
        // This is in case login detection fails or hasn't run yet.
        if (!_loggedIn)
        {
            // Show a dialog window because an overlay dialog cannot render on top of NativeWebView.
            // Be careful not to show dialog window when InputBlocker is active, or user won't be able to interact with the dialog.
            var options = new ConfirmationOptions(
                Title: "No Login Detected",
                Message: "You must be logged in to Nexus to download mods.\nDo you want to begin download anyway?");
            if (!await _navigator.ShowDialogWindowAsync<ConfirmationWindow, MainWindow, bool>(parameter: options, cancellationToken: cancellationToken))
                return;
        }

        string? errorMessage = null;
        try
        {
            // Prevent interactions with the WebView while downloading
            BlockInput = true;

            // 1. Download modlist and resolve download links
            FrozenDictionary<Hash, DownloadContext> modlist;
            try
            {
                modlist = await ResolveModList(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errorMessage = $"An unexpected error occurred while downloading and extracting modlist: {ex.Message}";
                throw;
            }

            // 2. Scan download folder for existing files and delete irrelevant files
            try
            {
                TotalProgress = new ProgressDisplay<long>("Scanning download folder");
                await ScanDownloadFolder(modlist, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                errorMessage = "The application does not have permission to access the download folder. Please check the folder permissions and try again.";
                throw;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errorMessage = $"An unexpected error occurred while scanning download folder: {ex.Message}";
                throw;
            }

            // 3. Download mods in parallel
            try
            {
                var part = GetPart(modlist.Values, _settings.CurrentPart, _settings.TotalParts);
                TotalProgress = new ProgressDisplay<long>(0, part.Length, "Downloading mods");
                _monitor = new(_settings.Timeout);

                var options = new ParallelOptions()
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = _settings.MaxConcurrency
                };
                await Parallel.ForEachAsync<DownloadContext>(source: MemoryMarshal.ToEnumerable(part), parallelOptions: options, body: DownloadItemAsync);
            }
            catch (WebViewDownloadException ex)
            {
                errorMessage = GenerateWebViewExceptionMessage(ex);
                throw;
            }
            catch (AggregateException ae) when (ae.InnerExceptions is [WebViewDownloadException ex])
            {
                errorMessage = GenerateWebViewExceptionMessage(ex);
                throw;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errorMessage = $"An unexpected error occurred while downloading mods: {ex.Message}";
                throw;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Download process is canceled by the user or by the application.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download process failed due to an exception.");
        }
        finally
        {
            _downloadCount = 0;
            TotalProgress = null;
            DownloadProgresses.Clear();
            await _monitor.DisposeAsync();
            BlockInput = false;
        }

        if (errorMessage is not null)
        {
            _logger.LogInformation("Displayed error: {ErrorMessage}", errorMessage);
            var options = new ToastOptions(errorMessage, ToastType.Error);
            // CancellationToken.None: error toasts must always be shown regardless of cancellation state.
            await _navigator.ShowPopupAsync<Toast>(parameter: options, verticalPlacement: Avalonia.Layout.VerticalAlignment.Bottom, cancellationToken: CancellationToken.None);
        }
        else if (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Download process completed successfully.");
            var options = new ToastOptions("All downloads completed!", ToastType.Success);
            // Don't await so the return button can be shown
            _ = _navigator.ShowPopupAsync<Toast>(parameter: options, verticalPlacement: Avalonia.Layout.VerticalAlignment.Bottom, cancellationToken: cancellationToken);
        }

        static string GenerateWebViewExceptionMessage(WebViewDownloadException ex) => ex.InterruptReason switch
        {
            WebViewDownloadInterruptReason.ServerUnauthorized =>
                "Download failed due to authentication error. Please make sure you are logged in to Nexus Mods and have permissions to download mods.",
            WebViewDownloadInterruptReason.ServerForbidden =>
                "Nexus Mods has blocked your requests. Please stop downloading and try again later; continuing right now may result in a temporary account suspension.",
            WebViewDownloadInterruptReason.DownloadProcessCrashed =>
                "The app has issues finding the download button or WebView has crashed. Please report this issue to the developer.",
            _ => $"An unexpected error occurred while downloading mods: {ex.Message}"
        };
    }

    /// <summary>
    /// Scan download folder for files relevant to selected modlist, verify them, and mark them as completed. Optionally, delete irrelevant files.
    /// </summary>
    private async Task ScanDownloadFolder(FrozenDictionary<Hash, DownloadContext> modlist, CancellationToken cancellationToken)
    {
        var folderPath = DownloadFolderPath ?? throw new InvalidOperationException("Download folder path is null.");

        if (!Directory.Exists(folderPath))
        {
            _logger.LogDebug("Download folder does not exist. Creating folder at '{FolderPath}'.", folderPath);
            Directory.CreateDirectory(folderPath);
            return;
        }

        _logger.LogInformation("Scanning download folder for existing files at '{FolderPath}'.", folderPath);
        int totalFiles = 0, matchedFiles = 0, deletedFiles = 0;
        var options = new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = _settings.MaxConcurrency
        };
        await Parallel.ForEachAsync(Directory.EnumerateFiles(folderPath), options, ScanFileAsync);
        _logger.LogInformation("Finished scanning {TotalFiles} files in '{FolderPath}'. Matched files: {MatchedFiles}; Deleted files: {DeletedFiles}.", totalFiles, folderPath, matchedFiles, deletedFiles);

        async ValueTask ScanFileAsync(string filePath, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref totalFiles);

            var extension = Path.GetExtension(filePath.AsSpan());
            var isRelevant = extension.Equals(".rar", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".7z", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".omod", StringComparison.OrdinalIgnoreCase);

            if (!isRelevant)
            {
                if (_settings.ClearDownloadFolder)
                {
                    _logger.LogDebug("Found existing file that is not relevant to the modlist. Deleting file '{LocalFileName}'.", Path.GetFileName(filePath));
                    Interlocked.Increment(ref deletedFiles);
                    File.Delete(filePath);
                }
                return;
            }

            Hash hash;
            await using (var stream = File.OpenRead(filePath))
            {
                hash = await stream.Hash(token: cancellationToken);
            }

            if (modlist.TryGetValue(hash, out var context) && context.TryVerify(hash))
            {
                _logger.LogDebug("Found an existing file '{LocalFileName}' that matches the expected hash. Marking the download as completed for '{FileName}'.", Path.GetFileName(filePath), context.Download.FileName);
                Interlocked.Increment(ref matchedFiles);
            }
            else if (_settings.ClearDownloadFolder)
            {
                _logger.LogDebug("Found existing file that is not relevant to the modlist. Deleting file '{LocalFileName}'.", Path.GetFileName(filePath));
                Interlocked.Increment(ref deletedFiles);
                File.Delete(filePath);
            }
        }
    }

    /// <summary>
    /// Download a single mod file by driving <paramref name="downloadContext"/> through its state machine with an internal retry loop.
    /// Intended to be called as a <see cref="Parallel.ForEachAsync{TSource}"/> body.
    /// </summary>
    /// <remarks>
    /// <para><b>Pre-download checks:</b> Items already in <see cref="DownloadState.Completed"/>, in an unexpected
    /// non-<see cref="DownloadState.Idle"/> state, or exceeding <see cref="_fileSizeLimit"/> are skipped immediately.</para>
    /// <para><b>Two-phase retry loop</b> — the loop runs while <c>phase != Complete</c> and <c>retryCount &lt;= MaxRetries</c>:</para>
    /// <list type="bullet">
    /// <item>
    /// <b>Phase 1 – Request:</b> Enters a <see cref="SemaphoreSlim"/>, sets current request, and navigates to the mod's URL.
    /// The WebView will either raise <see cref="OnDownloadStarting"/> (success) or <see cref="OnWebMessageReceived"/> (failure),
    /// and transition to <b>Download</b> with a completion task. If the request times out and throws a <see cref="TimeoutException"/>,
    /// consumes a retry slot and goes back to <b>Request</b>. The semaphore is always released in <c>finally</c>.
    /// </item>
    /// <item>
    /// <b>Phase 2 – Download:</b> Awaits the <c>completionTask</c> produced by <see cref="DownloadContext.TrySetOperation"/>
    /// or <see cref="DownloadContext.TryResume"/>. The method returns on Completed/Failed/Canceled states, retries on Paused/Retrying states,
    /// and throws on Fatal state. Unexpected states are logged and treated as failures.
    /// </item>
    /// </list>
    /// <para><b>Retry exhaustion:</b> When <c>retryCount</c> exceeds <c>MaxRetries</c>, <c>CanRetryAsync</c> returns
    /// <see langword="false"/> immediately. The loop guard <c>retryCount &lt;= MaxRetries</c> then
    /// terminates the loop on the next iteration check.</para>
    /// <para><b>Cleanup:</b> The <c>finally</c> block unconditionally disposes <paramref name="downloadContext"/>,
    /// removes its progress display from the UI, and increments the total progress counter.</para>
    /// </remarks>
    private async ValueTask DownloadItemAsync(DownloadContext downloadContext, CancellationToken cancellationToken)
    {
        var fileName = downloadContext.Download.FileName;
        var fileSize = downloadContext.Download.FileSize;

        // Skip items that were marked as completed in ScanDownloadFolder
        if (downloadContext.State == DownloadState.Completed)
        {
            _logger.LogDebug("Download item has already completed. Skipping download for '{FileName}'.", fileName);
            ReportProgress();
            return;
        }

        // Skip items that had mistakenly transitioned from Idle to something other than Completed and log a warning
        if (downloadContext.State != DownloadState.Idle)
        {
            _logger.LogWarning("Download item has unexpected state '{State}' before download begins. Skipping download for '{FileName}'.", downloadContext.State, fileName);
            ReportProgress();
            return;
        }

        // Skip items that are larger than file size limit
        if (fileSize > _fileSizeLimit)
        {
            _logger.LogDebug("Download item with size {FileSize}B is larger than allowed threshold {SizeLimit}B. Skipping download for '{FileName}'.", fileSize, _fileSizeLimit, fileName);
            ReportProgress();
            return;
        }

        var retryOptions = _settings.RetryOptions;
        int retryCount = 0;
        int delay = retryOptions.BaseDelay;

        var phase = DownloadPhase.Request;
        Task<DownloadState>? completionTask = null;
        Task retryTask = Task.CompletedTask;

        try
        {
            // Begin state machine with retry loop
            while (phase != DownloadPhase.Complete && retryCount <= retryOptions.MaxRetries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await retryTask.ConfigureAwait(false);

                switch (phase)
                {
                    // Phase 1: Send download request and wait for download to start
                    case DownloadPhase.Request:
                        _logger.LogTrace("Queueing download request for '{FileName}'.", fileName);
                        await _downloadRequestLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
                        try
                        {
                            var downloadRequestSource = new TaskCompletionSource<Task<DownloadState>>(TaskCreationOptions.RunContinuationsAsynchronously);
                            _currentRequest = new(downloadContext, downloadRequestSource, cancellationToken);
                            Address = downloadContext.Download.Uri;

                            completionTask = await downloadRequestSource.Task
                                .WaitAsync(TimeSpan.FromSeconds(_settings.Timeout), cancellationToken)
                                .ConfigureAwait(false);

                            phase = DownloadPhase.Download;
                        }
                        catch (TimeoutException)
                        {
                            // Retry if timeout
                            if (CanRetry("download request timed out"))
                            {
                                phase = DownloadPhase.Request;
                            }
                        }
                        finally
                        {
                            // Release resource so the next download request can start
                            _currentRequest = null;
                            _downloadRequestLimiter.Release();
                        }
                        break;

                    // Phase 2: Wait for download to complete and handle result
                    case DownloadPhase.Download:
                        _logger.LogTrace("Awaiting download result for '{FileName}'.", fileName);
                        switch (await completionTask!.ConfigureAwait(false)) // completionTask is set in Request phase or in Paused case
                        {
                            case DownloadState.Completed:
                                _logger.LogDebug("File downloaded and verified successfully for '{FileName}'.", fileName);
                                phase = DownloadPhase.Complete;
                                return;
                            case DownloadState.Paused:
                                if (CanRetry($"download was paused ('{downloadContext.InterruptReason}')"))
                                {
                                    if (!downloadContext.TryResume(_settings.VerifyDownloads, _monitor, cancellationToken, out completionTask))
                                    {
                                        _logger.LogWarning("Unable to resume due to '{InterruptReason}'. Attempting a fresh download for '{FileName}'.", downloadContext.InterruptReason, fileName);
                                        // Retry from the beginning in case resume status has changed while waiting for retry
                                        phase = DownloadPhase.Request;
                                    }
                                }
                                break;
                            case DownloadState.Retrying:
                                if (CanRetry($"download was interrupted ('{downloadContext.InterruptReason}')"))
                                    phase = DownloadPhase.Request;
                                break;
                            case DownloadState.Failed:
                            case DownloadState.Canceled:
                                _logger.LogDebug("Download failed or canceled due to '{InterruptReason}' for '{FileName}'.", downloadContext.InterruptReason, fileName);
                                return;
                            case DownloadState.Fatal:
                                throw new WebViewDownloadException(downloadContext.InterruptReason, fileName);
                            case DownloadState.Downloading:
                            case DownloadState.Idle:
                                throw new UnreachableException($"completeTask resolved with internal state '{downloadContext.State}' for '{fileName}'.");
                            default:
                                _logger.LogWarning("Download item has unexpected state '{State}'. Skipping download for '{FileName}'.", downloadContext.State, fileName);
                                return;
                        }
                        break;
                    case DownloadPhase.Complete:
                        throw new UnreachableException($"DownloadItemAsync did not exit after Complete phase for '{fileName}'.");
                    default:
                        _logger.LogWarning("Download method has entered unexpected phase '{Phase}'. Skipping download for '{FileName}'.", phase, fileName);
                        return;
                }
            }
        }
        finally
        {
            // Clean up unmanaged resources and progress display regardless of how the download process ended
            downloadContext.Dispose();
            Dispatcher.UIThread.Post(() => DownloadProgresses.Remove(downloadContext.Progress)); // Added in OnDownloadStarting
            ReportProgress();
        }

        void ReportProgress() => TotalProgress?.Report(Interlocked.Increment(ref _downloadCount));

        bool CanRetry(string reason)
        {
            // Dispose of download context to clean up unmanaged resources if we run out of retry attempts
            if (++retryCount > retryOptions.MaxRetries)
            {
                _logger.LogWarning("Download failed because {Reason}. Max retries reached. Terminating download for '{FileName}'.", reason, fileName);
                return false;
            }

            delay = retryOptions.GetNextDelay(delay, out var actualDelay);
            _logger.LogWarning("Download failed because {Reason}. Attempting {RetryCount} retry in {Delay} milliseconds for '{FileName}'.",
                reason, retryCount.DisplayWithSuffix(), actualDelay, fileName);
            retryTask = Task.Delay(actualDelay, cancellationToken);
            return true;
        }
    }

    /// <summary>
    /// Handle web messages for current download request, indicating that something went wrong with the request.
    /// </summary>
    private void OnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        if (_currentRequest is not (var context, var tcs, _))
        {
            _logger.LogWarning("Received a web message '{Message}' when there's no ongoing download request. Message ignored.", e.Body);
            return;
        }

        // Json web message. Should be a pure string containing status report.
        var message = e.Body;
        var fileName = context.Download.FileName;
        Task<DownloadState>? completeTask;

        // Handle messages by setting the download request task result to fail with the appropriate reason,
        // which will be caught in DownloadItemAsync and handled accordingly.
        // If unable to set failure or unknown message, then let the request time out naturally.
        switch (message)
        {
            case "NOT_FILE_PAGE":
                _logger.LogDebug("Received web message '{webMessage}' — Current page is not a Nexus file download page. Requested download: '{FileName}'.", message, fileName);
                if (context.TrySetFailure(DownloadState.Failed, WebViewDownloadInterruptReason.NetworkInvalidRequest, out completeTask))
                    tcs.SetResult(completeTask);
                break;

            case "AUTH_REQUIRED":
                _logger.LogDebug("Received web message '{webMessage}' — User is not logged in or lacks credentials. Requested download: '{FileName}'.", message, fileName);
                if (context.TrySetFailure(DownloadState.Fatal, WebViewDownloadInterruptReason.ServerUnauthorized, out completeTask))
                    tcs.SetResult(completeTask);
                break;

            case "TOO_MANY_REQUESTS":
                _logger.LogDebug("Received web message '{webMessage}' — Nexus Mods rate limit exceeded. Requested download: '{FileName}'.", message, fileName);
                if (context.TrySetFailure(DownloadState.Fatal, WebViewDownloadInterruptReason.ServerForbidden, out completeTask))
                    tcs.SetResult(completeTask);
                break;

            case "BUTTON_NOT_FOUND":
                _logger.LogDebug("Received web message '{webMessage}' — Download button cannot be found. Requested download: '{FileName}'.", message, fileName);
                if (context.TrySetFailure(DownloadState.Fatal, WebViewDownloadInterruptReason.DownloadProcessCrashed, out completeTask))
                    tcs.SetResult(completeTask);
                break;

            default:
                _logger.LogDebug("Received unregconized web message '{webMessage}'. Requested download: '{FileName}'.", message, fileName);
                break;
        }
    }

    /// <summary>
    /// Listen for download starting event, indiciating that the current download request was successful.
    /// Set context operation to start tracking the download progress, and mark the download request task as completed.
    /// </summary>
    [RelayCommand]
    private void OnDownloadStarting(DownloadStartingEventArgs args)
    {
        // Make sure download dialog is never shown
        args.Handled = true;

        if (_currentRequest is not (var context, var tcs, var token))
        {
            _logger.LogWarning("Received a download starting event when there's no ongoing request. Download rejected: {ResultFilePath}", args.ResultFilePath);
            args.Cancel = true;
            return;
        }

        _logger.LogDebug("Received download starting event. Setting up download context and progress tracking for '{FileName}'.", context.Download.FileName);

        // Set download context operation to update context status to downloading
        var operation = args.DownloadOperation;

        // Filter out downloads that are not triggered by current request
        if (!IsExpectedDownload(context.Download, operation.ResultFilePath, operation.TotalBytesToReceive))
        {
            _logger.LogWarning("Received a stray download starting event while waiting for '{FileName}' to start downloading. Download rejected: '{StrayFileName}'.",
                context.Download.FileName, Path.GetFileName(operation.ResultFilePath));
            args.Cancel = true;
            operation.Dispose();
            return;
        }

        // Bind the download operation to the context, transitioning the context to downloading state
        if (!context.TrySetOperation(operation, _settings.VerifyDownloads, _monitor, token, out var completionTask))
        {
            _logger.LogWarning("Duplicated download detected. Canceling download operation for '{FileName}'.", context.Download.FileName);
            args.Cancel = true;
            operation.Dispose();
            return;
        }

        // Add progress display from the UI thread (INotifyCollectionChanged)
        Dispatcher.UIThread.Post(() => { if (!DownloadProgresses.Contains(context.Progress)) DownloadProgresses.Add(context.Progress); });

        // Mark the download request task as completed
        tcs.SetResult(completionTask);
    }

    /// <summary>
    /// Determines whether an incoming download event corresponds to the expected download request.
    /// This method favors false positives over false negatives.
    /// </summary>
    /// <param name="download">The download that is expected to arrive.</param>
    /// <param name="resultFilePath">Result file path of incoming download.</param>
    /// <param name="contentLength">Byte length of incoming download.</param>
    /// <returns><see langword="true"/> if is a match; otherwise <see langword="false"/>.</returns>
    private bool IsExpectedDownload(NexusDownload download, string resultFilePath, long? contentLength)
    {
        var incomingName = Path.GetFileNameWithoutExtension(resultFilePath.AsSpan());
        var expectedName = Path.GetFileNameWithoutExtension(download.FileName.AsSpan());

        // Handle the simplest cast where names match exactly
        if (expectedName.Equals(incomingName, StringComparison.OrdinalIgnoreCase))
            return true;

        // Apply fuzzy match and fall back to content length if fuzzy match fails
        if (IsFuzzyMatch(expectedName, incomingName))
            return true;

        if (contentLength is > 0 && contentLength == download.FileSize)
            return true;

        return false;

        // Matches by mod_id and Unix timestamp. Returns false if no timestamp can be extracted.
        bool IsFuzzyMatch(ReadOnlySpan<char> expectedFileName, ReadOnlySpan<char> incomingFileName)
        {
            var expectedStamp = ExtractUnixStamp(expectedFileName);

            if (expectedStamp.IsWhiteSpace())
                return false;

            return incomingFileName.Contains(expectedStamp, StringComparison.OrdinalIgnoreCase)
                && incomingFileName.Contains(download.ModID, StringComparison.OrdinalIgnoreCase);
        }

        ReadOnlySpan<char> ExtractUnixStamp(ReadOnlySpan<char> fileName)
        {
            int lastHyphen = fileName.LastIndexOf('-');
            if (lastHyphen == -1)
                return default;

            ReadOnlySpan<char> potentialId = fileName[(lastHyphen + 1)..];

            if (potentialId.Length >= 9 && potentialId.Length <= 11)
            {
                foreach (char c in potentialId)
                {
                    if (!char.IsDigit(c))
                        return default;
                }
                return potentialId;
            }

            return default;
        }
    }

    /// <summary>
    /// Detect login status and execute auto download script
    /// </summary>
    [RelayCommand]
    private void OnNavigationCompleted(Uri address)
    {
        // Execute the auto-download script if we're trying to download a file
        if (_currentRequest is { Context: DownloadContext context })
        {
            _logger.LogTrace("Executing download script for '{FileName}' on '{Uri}'", context.Download.FileName, address);
            _ = JSRunner.ExecuteScriptAsync(AutoDownloadScript);
        }
        // Else check for login status
        else if (address.AbsoluteUri == AccountSecurityUrl)
        {
            _logger.LogInformation("User has successfully logged in.");
            _loggedIn = true;
        }
        // Signal that this method has run at least once
        if (!_loginChecked)
        {
            _logger.LogTrace("Login status checked. Setting 'loginChecked' flag to true.");
            _loginChecked = true;
            DownloadCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Enable download command after <see cref="OnNavigationCompleted"/> has run so that we can detect login status.
    /// This helps prevent premature interactions where the user is already logged in but the app hasn't detected it yet,
    /// leading to a popup warning about not being logged in.
    /// </summary>
    private bool CanExecuteDownloadCommand() => _loginChecked;

    /// <summary>
    /// Represent a pending download request with a TaskCompletionSource that will be completed when the
    /// download starts and a cancellation token to cancel the request.
    /// </summary>
    /// NOTE: This must remain a reference type in order to guarantee write atomicity.
    private record DownloadRequest(DownloadContext Context, TaskCompletionSource<Task<DownloadState>> TCS, CancellationToken Token);
    #endregion
}

file enum DownloadPhase
{
    /// <summary>
    /// Download request is waiting to be sent or is pending.
    /// </summary>
    Request,
    /// <summary>
    /// Download has started and is in progress.
    /// </summary>
    Download,
    /// <summary>
    /// Download has completed.
    /// </summary>
    Complete
}
