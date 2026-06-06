using Avalonia.Threading;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using WabbajackDownloader.Common.Hashing;
using WabbajackDownloader.Common.Progress;
using WabbajackDownloader.Features.WabbajackModList;
using WabbajackDownloader.Features.WebView;

namespace WabbajackDownloader.Features.Dashboard;

partial class DownloadPageViewModel
{
    /// <summary>
    /// Manage the lifecycle of a download operation by maintaining an internal state machine.
    /// Possible states are Idle, Downloading, Paused, Completed, Retrying, Canceled, Failed, and Fatal, as described in <see cref="DownloadState"/>.
    /// <list type="bullet">
    /// <item>
    /// Idle is the initial state, indicating that no download operation has been associated with this context yet.
    /// The Idle state can transition to Downloading via <see cref="TrySetOperation"/>, to Completed via <see cref="TryVerify"/>, or to Failed/Fatal via <see cref="TrySetFailure"/>.
    /// </item>
    /// <item>
    /// The Downloading state indicates that a download operation is in progress. It can transition to Paused if the download is interrupted but can be resumed,
    /// to Retrying/Failed/Fatal if the download is interrupted and cannot be resumed, to Canceled if the download is canceled via the provided <see cref="CancellationToken"/>,
    /// or to Completed if the download finishes successfully. Apart from the cancellation scenario, all transitions out of Downloading happen internally and
    /// can be observed by awaiting the <see cref="Task"/> produced by <see cref="TrySetOperation"/> or <see cref="TryResume"/>.
    /// </item>
    /// <item>
    /// The Paused state indicates that the download is currently paused and resumable. It can transition back to Downloading via <see cref="TryResume"/>.
    /// </item>
    /// <item>
    /// The Retrying state indicates that the download has failed but is eligible for a retry. It can transition to Downloading via <see cref="TrySetOperation"/>.
    /// </item>
    /// <item>
    /// The Canceled, Failed, Fatal, and Completed states are terminal states. No transition is allowed out of these states.
    /// </item>
    /// </list>
    /// </summary>
    private sealed class DownloadContext(NexusDownload nexusDownload) : IDisposable
    {
        /// <summary>
        /// The download item that this context represents.
        /// </summary>
        public NexusDownload Download { get; init; } = nexusDownload;

        /// <summary>
        /// The state of this download context.
        /// </summary>
        public DownloadState State
        {
            get => _state;
            private set => _state = value;
        }
        private volatile DownloadState _state = DownloadState.Idle;

        /// <summary>
        /// The reason for download pause, interruption, cancellation, or failure, if applicable.
        /// </summary>
        public WebViewDownloadInterruptReason InterruptReason { get; private set; } = WebViewDownloadInterruptReason.None;

        /// <summary>
        /// The progress of the current download.
        /// </summary>
        public DownloadProgressDisplay Progress { get; } = new(nexusDownload.FileSize, nexusDownload.FileName);

        /// <summary>
        /// Used to track the timestamp of the most recent byte received event. This is used to determine if a download is stalled.
        /// </summary>
        public long ByteReceivedTimeStamp
        {
            get;
            private set => Volatile.Write(ref field, value);
        }

        /// <summary>
        /// Used to get the current timestamp.
        /// </summary>
        private readonly TimeProvider _timeProvider = TimeProvider.System;

        /// <summary>
        /// The download operation of this context.
        /// Operation is not <see langword="null"/> when <see cref="State"/> is
        /// <see cref="DownloadState.Downloading"/> or <see cref="DownloadState.Paused"/>.
        /// </summary>
        private DownloadOperation? _operation;

        /// <summary>
        /// Track the status of the native download operation. This is tightly coupled with the DownloadOperation and should be reset whenever a new operation is assigned.
        /// </summary>
        private TaskCompletionSource<(DownloadState, string)> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// The cancellation token registration associated with the download operation.
        /// </summary>
        private CancellationTokenRegistration _cancellationRegistration;

        private long? _lastResumeBytesReceived;

        /// <summary>
        /// Attempt to verify by comparing the provided file's hash to the expected hash.
        /// </summary>
        /// <remarks>
        /// This is useful for verifying files that already exists on disk, and is intended to be called when <see cref="State"/> is <see cref="DownloadState.Idle"/>.
        /// Calling this in <see cref="DownloadState.Completed"/> will always return <see langword="true"/>.
        /// Calling this in any other state will always return <see langword="false"/>.
        /// </remarks>
        /// <returns><see langword="true"/> if the file is verified successfully; otherwise, <see langword="false"/>.</returns>
        public bool TryVerify(Hash computedHash)
        {
            if (State == DownloadState.Completed)
                return true;

            if (State != DownloadState.Idle)
                return false;

            if (computedHash.Equals(Download.Hash))
            {
                State = DownloadState.Completed;
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Attempt to transition to a failure state with the provided interrupt reason.
        /// </summary>
        /// <remarks>
        /// This is useful for handling failures that are detected outside of the native download operation, such as before
        /// download begins, and is intended to be called when <see cref="State"/> is <see cref="DownloadState.Idle"/>.
        /// Calling this in any other state will always return <see langword="false"/>.
        /// The provided state must be either <see cref="DownloadState.Failed"/> or <see cref="DownloadState.Fatal"/>;
        /// otherwise, this method will return <see langword="false"/>.
        /// </remarks>
        /// <returns><see langword="true"/> when transition succeeds; otherwise, <see langword="false"/>.</returns>
        public bool TrySetFailure(DownloadState state, WebViewDownloadInterruptReason interruptReason, [NotNullWhen(true)] out Task<DownloadState>? completeTask)
        {
            if (State == DownloadState.Idle && (state == DownloadState.Failed || state == DownloadState.Fatal))
            {
                InterruptReason = interruptReason;
                State = state;
                completeTask = Task.FromResult(State);
                return true;
            }
            completeTask = null;
            return false;
        }

        /// <summary>
        /// Pause a download that is currently in progress. This acts as a soft reset for stalled downloads.
        /// Downloads that are not in progress cannot be paused and will be ignored.
        /// </summary>
        public void Pause()
        {
            if (_operation is DownloadOperation operation)
            {
                // Calling Pause is a safe no-op when CoreWebView2DownloadState.Completed or CoreWebView2DownloadState.Interrupted
                Dispatcher.UIThread.Post(static op =>
                {
                    try { ((DownloadOperation)op!).Pause(); } catch { }
                }, operation);
            }
        }

        /// <summary>
        /// Set the operation and wire up event handlers to update status and progress.
        /// Operation can only be set when when <see cref="State"/> is <see cref="DownloadState.Idle"/> or <see cref="DownloadState.Retrying"/>.
        /// </summary>
        /// <remarks>
        /// A <see cref="DownloadContext"/> having an assigned operation is synonymous to having an ongoing download.
        /// An ongoing download cannot be replaced with another one; it can only run to completion, paused, canceled, or failed.
        /// Once operation is set, the context will update its <see cref="State"/> and <see cref="Progress"/>
        /// based on the operation's events until the download completes, fails, or gets canceled.
        /// </remarks>
        /// <param name="completeTask">When the operation is successfully set, this will be assigned a task that completes when
        /// <see cref="State"/> changes from <see cref="DownloadState.Downloading"/> to another. Otherwise, it will be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the operation was set; otherwise, <see langword="false"/>.</returns>
        public bool TrySetOperation(DownloadOperation operation, bool verifyDownloads, DownloadMonitor monitor,
            CancellationToken token, [NotNullWhen(true)] out Task<DownloadState>? completeTask)
        {
            completeTask = null;

            if (_operation is not null)
                return false;

            if (State != DownloadState.Idle && State != DownloadState.Retrying)
                return false;

            // TCS is coupled to the DownloadOperation. If we assign a new operation, make sure TCS is fresh.
            _tcs = new TaskCompletionSource<(DownloadState, string)>(TaskCreationOptions.RunContinuationsAsynchronously);
            var downloadTask = _tcs.Task;

            // TCS is tracking the download operation only. We should hand back a task that includes verification.
            completeTask = VerifyDownloadAsync(downloadTask, verifyDownloads, token);

            // Reset progress for retry
            Progress.ResetProgress();

            // Hook onto native events
            _operation = operation;
            operation.StateChanged += OnDownloadStateChanged;
            operation.BytesReceivedChanged += OnBytesReceivedChanged;

            // Cancel the native download operation if the provided cancellation token is triggered.
            _cancellationRegistration = token.Register(
                static state => Dispatcher.UIThread.Post(static op =>
                {
                    try { ((DownloadOperation)op!).Cancel(); } catch { }
                }, state),
                operation);

            // Once the operation is set, we can consider the download as started.
            InterruptReason = WebViewDownloadInterruptReason.None;
            State = DownloadState.Downloading;

            // Register the download with the monitor so that it can track for stalls.
            // We do this after setting state and timestamp to prevent monitor from immediately unregistering or flagging the download as stalled.
            ByteReceivedTimeStamp = _timeProvider.GetTimestamp();
            monitor.Register(this);

            return true;
        }

        /// <summary>
        /// Attempt to resume a download that is currently paused.
        /// </summary>
        /// <remarks>
        /// Calling this method on a non-resumable download or when state is not <see cref="DownloadState.Paused"/> will always return <see langword="false"/>.
        /// </remarks>
        /// <param name="completeTask">When the download is successfully resumed, this will be assigned a task that completes when
        /// <see cref="State"/> changes from <see cref="DownloadState.Downloading"/> to another. Otherwise, it will be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the download was resumed; otherwise, <see langword="false"/>.</returns>
        public bool TryResume(bool verifyDownloads, DownloadMonitor monitor, CancellationToken token, [NotNullWhen(true)] out Task<DownloadState>? completeTask)
        {
            completeTask = null;

            if (_operation is DownloadOperation operation && operation.CanResume)
            {
                if (_lastResumeBytesReceived is long previous)
                {
                    var current = operation.BytesReceived;
                    // This means that the download is still stalled despite previous resume attempt.
                    // The fault lies with Chromium download manager being unable to resume a half-open TCP connection,
                    // but still mistakenly flag the download operation as resumable.
                    // Run cleanup logic and set state to Canceled.
                    if (previous == current)
                    {
                        InterruptReason = operation.InterruptReason;
                        State = DownloadState.Retrying;
                        Dispose();
                        return false;
                    }
                }

                // Bookmark the bytes received at the time of resume so that we can detect if the download is still stalled when TryResume is called again
                _lastResumeBytesReceived = operation.BytesReceived;

                // Make a new TaskCompletionSource since the old one is already completed when the download was paused
                _tcs = new TaskCompletionSource<(DownloadState, string)>(TaskCreationOptions.RunContinuationsAsynchronously);
                var downloadTask = _tcs.Task;
                completeTask = VerifyDownloadAsync(downloadTask, verifyDownloads, token);

                // Re-register cancellation using a new token
                _cancellationRegistration.Dispose();
                _cancellationRegistration = token.Register(
                    static state => Dispatcher.UIThread.Post(static op =>
                    {
                        try { ((DownloadOperation)op!).Cancel(); } catch { }
                    }, state),
                    operation);

                // Resume download
                Dispatcher.UIThread.Post(static op =>
                {
                    try { ((DownloadOperation)op!).Resume(); } catch { }
                }, operation);

                // Update state
                InterruptReason = WebViewDownloadInterruptReason.None;
                State = DownloadState.Downloading;

                // Register with monitor
                ByteReceivedTimeStamp = _timeProvider.GetTimestamp();
                monitor.Register(this);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Verify that the completed download matches the expected hash and delete the file if it is corrupted.
        /// This is intended to be called after the native download operation reports completion, but before we report completion to the user.
        /// </summary>
        private async Task<DownloadState> VerifyDownloadAsync(Task<(DownloadState, string)> downloadTask, bool verifyDownloads, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            (var result, var filePath) = await downloadTask;

            // Downloading state is an indication that the download has completed and is pending verification
            if (result == DownloadState.Downloading)
            {
                if (!verifyDownloads || await ComputeHashAsync(filePath, token))
                {
                    token.ThrowIfCancellationRequested();

                    InterruptReason = WebViewDownloadInterruptReason.None;
                    State = DownloadState.Completed;
                    return State;
                }
                else
                {
                    token.ThrowIfCancellationRequested();

                    // Attempt to delete the corrupted file
                    try { File.Delete(filePath); } catch { }

                    InterruptReason = WebViewDownloadInterruptReason.FileHashMismatch;
                    State = DownloadState.Retrying;
                    return State;
                }
            }
            // Act as a passthrough for other states
            return result;
        }

        private async Task<bool> ComputeHashAsync(string filePath, CancellationToken token)
        {
            await using var stream = File.OpenRead(filePath);
            var hash = await stream.Hash(token: token);
            return hash.Equals(Download.Hash);
        }

        private void OnBytesReceivedChanged(object? sender, EventArgs args)
        {
            if (sender is not DownloadOperation op)
                return;

            ByteReceivedTimeStamp = _timeProvider.GetTimestamp();
            Progress.Report(op.BytesReceived);
        }

        private void OnDownloadStateChanged(object? sender, EventArgs args)
        {
            if (sender is not DownloadOperation op)
                return;

            var filePath = op.ResultFilePath;
            InterruptReason = op.InterruptReason;

            if (op.State == WebViewDownloadState.Completed)
            {
                // Use the Downloading state as an intermediate state to indicate that the download has finished but verification is still pending.
                // To the awaiter, it looks like the download is still in progress until verification completes. No need to set it since that's already the case.
                // State = DownloadState.Downloading;
                Dispose();
                _tcs.TrySetResult((State, filePath));
            }
            else if (op.State == WebViewDownloadState.Interrupted)
            {
                if (op.CanResume)
                {
                    State = DownloadState.Paused;
                    _tcs.TrySetResult((State, filePath));
                }
                else
                {
                    // User/System intervention
                    if (op.InterruptReason == WebViewDownloadInterruptReason.UserCanceled
                        || op.InterruptReason == WebViewDownloadInterruptReason.UserShutdown)
                        State = DownloadState.Canceled;
                    // Systemic failures
                    else if (op.InterruptReason == WebViewDownloadInterruptReason.ServerUnauthorized // Server did not authorize access to resource
                            || op.InterruptReason == WebViewDownloadInterruptReason.ServerForbidden // Server access forbidden
                            || op.InterruptReason == WebViewDownloadInterruptReason.FileNoSpace // Disk out of space
                            || op.InterruptReason == WebViewDownloadInterruptReason.FileBlockedByPolicy // Blocked by local policy
                            || op.InterruptReason == WebViewDownloadInterruptReason.FileAccessDenied // Blocked by antivirus or file permission
                            || op.InterruptReason == WebViewDownloadInterruptReason.ServerCertificateProblem // SSL certificate error or MITM attack
                            || op.InterruptReason == WebViewDownloadInterruptReason.DownloadProcessCrashed) // WebView crashed
                        State = DownloadState.Fatal;
                    // Item failures
                    else if (op.InterruptReason == WebViewDownloadInterruptReason.NetworkInvalidRequest
                            || op.InterruptReason == WebViewDownloadInterruptReason.FileNameTooLong
                            || op.InterruptReason == WebViewDownloadInterruptReason.FileTooLarge
                            || op.InterruptReason == WebViewDownloadInterruptReason.FileTooShort
                            || op.InterruptReason == WebViewDownloadInterruptReason.FileMalicious
                            || op.InterruptReason == WebViewDownloadInterruptReason.FileSecurityCheckFailed
                            || op.InterruptReason == WebViewDownloadInterruptReason.ServerCrossOriginRedirect)
                        State = DownloadState.Failed;
                    // Transient errors
                    else
                        State = DownloadState.Retrying;

                    Dispose();
                    _tcs.TrySetResult((State, filePath));
                }
            }
        }

        // Dispose of unmanaged resources and CancellationTokenRegistration
        public void Dispose()
        {
            if (_operation is DownloadOperation operation)
            {
                // Detach managed and COM event handlers first to prevent stale callbacks
                // from firing into a context that is already being torn down
                operation.StateChanged -= OnDownloadStateChanged;
                operation.BytesReceivedChanged -= OnBytesReceivedChanged;
                _operation = null;

                // Attempt to cancel in case the download is in progress or paused and dispose it
                Dispatcher.UIThread.Post(static op =>
                {
                    try
                    {
                        var nativeOp = (DownloadOperation)op!;
                        nativeOp.Cancel();
                        nativeOp.Dispose();
                    }
                    catch { }
                }, operation);
            }
            _cancellationRegistration.Dispose();
            _lastResumeBytesReceived = null;
        }
    }
}
