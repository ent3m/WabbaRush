using WabbajackDownloader.Features.WebView.Interop;

namespace WabbajackDownloader.Features.WebView;

/// <summary>
/// A wrapper around <see cref="ICoreWebView2DownloadStartingEventArgs"/> that controls a WebView2 download that is starting.
/// </summary>
internal class DownloadStartingEventArgs(ICoreWebView2DownloadStartingEventArgs args) : EventArgs
{
    /// <summary>
    /// Indicates whether to cancel the download.
    /// </summary>
    /// <remarks>
    /// If canceled, the download save dialog is not displayed regardless of the <see cref="Handled"/> 
    /// value and the state is changed to <see cref="WebViewDownloadState.Interrupted"/>
    /// with interrupt reason <see cref="WebViewDownloadInterruptReason.UserCanceled"/>.
    /// </remarks>
    public bool Cancel
    {
        get
        {
            _args.GetCancel(out var value);
            return value;
        }
        set
        {
            _args.PutCancel(value);
        }
    }

    /// <summary>
    /// Returns the <see cref="WabbajackDownloader.Features.WebView.DownloadOperation"/> for the download that has started.
    /// </summary>
    public DownloadOperation DownloadOperation
    {
        get
        {
            _args.GetDownloadOperation(out var operation);
            return new DownloadOperation(operation
                ?? throw new InvalidOperationException("Failed to get ICoreWebView2DownloadOperation. Did this download expire?"));
        }
    }

    /// <summary>
    /// Indicates whether to hide the default download dialog.
    /// </summary>
    /// <remarks>
    /// If set to <see langword="true"/>, the default download dialog is hidden for this download.
    /// The download progresses normally if it is not canceled, there will just be no default UI shown.
    /// By default the value is <see langword="false"/> and the default download dialog is shown.
    /// </remarks>
    public bool Handled
    {
        get
        {
            _args.GetHandled(out var value);
            return value;
        }
        set
        {
            _args.PutHandled(value);
        }
    }

    /// <summary>
    /// The path to the file.
    /// </summary>
    /// <remarks>
    /// If setting the path, the host should ensure that it is an absolute path, 
    /// including the file name, and that the path does not point to an existing file.
    /// If the path points to an existing file, the file will be overwritten. If the directory does not exist, it is created.
    /// </remarks>
    public string ResultFilePath
    {
        get
        {
            _args.GetResultFilePath(out var value);
            return value;
        }
        set
        {
            _args.PutResultFilePath(value);
        }
    }

    private readonly ICoreWebView2DownloadStartingEventArgs _args = args;
}
