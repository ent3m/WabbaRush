using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WabbajackDownloader.Features.WebView.Interop;

// This is the COM interface WebView2 calls back into when a download starts.
[GeneratedComInterface]
[Guid("efedc989-c396-41ca-83f7-07f845a55724")]
internal partial interface ICoreWebView2DownloadStartingEventHandler
{
    void Invoke(
        ICoreWebView2? sender,
        ICoreWebView2DownloadStartingEventArgs? args);
}

[GeneratedComClass]
internal partial class CoreWebView2DownloadStartingHandler : ICoreWebView2DownloadStartingEventHandler
{
    private readonly Action<ICoreWebView2DownloadStartingEventArgs?> _callback;

    public CoreWebView2DownloadStartingHandler(Action<ICoreWebView2DownloadStartingEventArgs?> callback)
        => _callback = callback;

    public void Invoke(ICoreWebView2? sender, ICoreWebView2DownloadStartingEventArgs? args)
        => _callback(args);
}