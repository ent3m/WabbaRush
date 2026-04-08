using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WabbajackDownloader.Features.NexusMods.Interop;

// This is the COM interface WebView2 calls back into when a download starts.
[GeneratedComInterface]
[Guid("efedc989-c396-41ca-83f7-07f845a55724")]
internal partial interface ICoreWebView2DownloadStartingEventHandler
{
    // WebView2 calls this from its side
    void Invoke(
        ICoreWebView2? sender,
        ICoreWebView2DownloadStartingEventArgs? args);
}

[GeneratedComClass]
internal partial class DownloadStartingHandler : ICoreWebView2DownloadStartingEventHandler
{
    private readonly Action<ICoreWebView2DownloadStartingEventArgs?> _callback;

    public DownloadStartingHandler(Action<ICoreWebView2DownloadStartingEventArgs?> callback)
        => _callback = callback;

    public void Invoke(ICoreWebView2? sender, ICoreWebView2DownloadStartingEventArgs? args)
        => _callback(args);
}