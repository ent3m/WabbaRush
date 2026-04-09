using System.Globalization;
using WabbajackDownloader.Features.WebView.Interop;

namespace WabbajackDownloader.Features.WebView;

/// <summary>
/// A wrapper around <see cref="ICoreWebView2DownloadOperation"/> that provides events and properties for monitoring and controlling a download operation in WebView2.
/// </summary>
internal class DownloadOperation : IDisposable
{
    /// <summary>
    /// Event raised when the bytes received count is updated.
    /// </summary>
    public event Action? BytesReceivedChanged;

    /// <summary>
    /// Event raised when the estimated end time changes.
    /// </summary>
    public event Action? EstimatedTimeEndChanged;

    /// <summary>
    /// Event raised when the state of the download changes.
    /// </summary>
    /// <remarks>
    /// Use <see cref="DownloadOperation.State"/> to get the current state,
    /// and <see cref="DownloadOperation.InterruptReason"/> to get the reason if the download is interrupted.
    /// </remarks>
    public event Action? StateChanged;

    /// <summary>
    /// The number of bytes that have been written to the download file.
    /// </summary>
    public long BytesReceived
    {
        get
        {
            _operation.GetBytesReceived(out var bytesReceived);
            return bytesReceived;
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> if an interrupted download can be resumed.
    /// </summary>
    /// <remarks>
    /// Downloads with the following interrupt reasons may automatically resume without you calling any methods:
    /// <see cref="WebViewDownloadInterruptReason.ServerNoRange"/>, <see cref="WebViewDownloadInterruptReason.FileHashMismatch"/>,
    /// <see cref="WebViewDownloadInterruptReason.FileTooShort"/>.
    /// In these cases progress may be restarted with <see cref="BytesReceived"/> set to 0.
    /// </remarks>
    public bool CanResume
    {
        get
        {
            _operation.GetCanResume(out var canResume);
            return canResume;
        }
    }

    /// <summary>
    /// The Content-Disposition header value from the download's HTTP response. If none, the value is an empty string.
    /// </summary>
    public string ContentDisposition
    {
        get
        {
            _operation.GetContentDisposition(out var contentDisposition);
            return contentDisposition;
        }
    }

    /// <summary>
    /// The estimated end time of the download.
    /// </summary>
    public DateTime EstimatedTimeEnd
    {
        get
        {
            _operation.GetEstimatedEndTime(out var estimatedTimeEnd);
            if (DateTime.TryParse(estimatedTimeEnd,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var estimatedDateTime))
                return estimatedDateTime;
            else
                return DateTime.MaxValue;
        }
    }

    /// <summary>
    /// The reason why connection with file host was broken.
    /// </summary>
    public WebViewDownloadInterruptReason InterruptReason
    {
        get
        {
            _operation.GetInterruptReason(out var interruptReason);
            return interruptReason;
        }
    }

    /// <summary>
    /// MIME type of the downloaded content.
    /// </summary>
    public string MimeType
    {
        get
        {
            _operation.GetMimeType(out var mimeType);
            return mimeType;
        }
    }

    /// <summary>
    /// The absolute path to the download file, including file name.
    /// </summary>
    public string ResultFilePath
    {
        get
        {
            _operation.GetResultFilePath(out var resultFilePath);
            return resultFilePath;
        }
    }

    /// <summary>
    /// The state of the download. A download can be in progress, interrupted, or completed.
    /// </summary>
    public WebViewDownloadState State
    {
        get
        {
            _operation.GetState(out var state);
            return state;
        }
    }

    /// <summary>
    /// The total bytes to receive count.
    /// </summary>
    public long? TotalBytesToReceive
    {
        get
        {
            _operation.GetTotalBytesToReceive(out var totalBytesToReceive);
            if (totalBytesToReceive == -1)
                return null;
            else
                return totalBytesToReceive;
        }
    }

    /// <summary>
    /// The URI of the download.
    /// </summary>
    public string Uri
    {
        get
        {
            _operation.GetUri(out var uri);
            return uri;
        }
    }

    /// <summary>
    /// Cancels the download.
    /// </summary>
    public void Cancel() => _operation.Cancel();

    /// <summary>
    /// Pauses the download.
    /// </summary>
    /// <remarks>
    /// If paused, the default download dialog shows that the download is paused. No effect if download is already paused.
    /// Pausing a download changes the state from <see cref="WebViewDownloadState.InProgress"/> to <see cref="WebViewDownloadState.Interrupted"/>,
    /// with interrupt reason set to <see cref="WebViewDownloadInterruptReason.UserCanceled"/>.
    /// </remarks>
    public void Pause() => _operation.Pause();

    /// <summary>
    /// Resumes a paused download.
    /// May also resume a download that was interrupted for another reason if <see cref="CanResume"/> returns <see langword="true"/>.
    /// </summary>
    public void Resume() => _operation.Resume();

    public DownloadOperation(ICoreWebView2DownloadOperation operation)
    {
        _operation = operation;

        _bytesReceivedChangedHandler = new(OnBytesReceivedChanged);
        _operation.AddBytesReceivedChanged(_bytesReceivedChangedHandler, out _bytesReceivedChangedToken);

        _estimatedEndTimeChangedHandler = new(OnEstimatedEndTimeChanged);
        _operation.AddEstimatedEndTimeChanged(_estimatedEndTimeChangedHandler, out _estimatedEndTimeChangedToken);

        _stateChangedHandler = new(OnStateChanged);
        _operation.AddStateChanged(_stateChangedHandler, out _stateChangedToken);
    }

    public void Dispose()
    {
        _operation.RemoveBytesReceivedChanged(_bytesReceivedChangedToken);
        _operation.RemoveEstimatedEndTimeChanged(_estimatedEndTimeChangedToken);
        _operation.RemoveStateChanged(_stateChangedToken);
    }

    private readonly ICoreWebView2DownloadOperation _operation;

    private readonly CoreWebView2BytesReceivedChangedHandler _bytesReceivedChangedHandler;
    private readonly EventRegistrationToken _bytesReceivedChangedToken;

    private readonly EstimatedEndTimeChangedHandler _estimatedEndTimeChangedHandler;
    private readonly EventRegistrationToken _estimatedEndTimeChangedToken;

    private readonly CoreWebView2StateChangedHandler _stateChangedHandler;
    private readonly EventRegistrationToken _stateChangedToken;

    private void OnBytesReceivedChanged(ICoreWebView2DownloadOperation? sender) =>
        BytesReceivedChanged?.Invoke();

    private void OnEstimatedEndTimeChanged(ICoreWebView2DownloadOperation? sender) =>
        EstimatedTimeEndChanged?.Invoke();

    private void OnStateChanged(ICoreWebView2DownloadOperation? sender) =>
        StateChanged?.Invoke();
}