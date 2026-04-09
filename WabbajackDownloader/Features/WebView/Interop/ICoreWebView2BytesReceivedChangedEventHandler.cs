using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WabbajackDownloader.Features.WebView.Interop;

[GeneratedComInterface]
[Guid("828e8ab6-d94c-4264-9cef-5217170d6251")]
internal partial interface ICoreWebView2BytesReceivedChangedEventHandler
{
    // args is always null for this event
    void Invoke(ICoreWebView2DownloadOperation? sender, nint args);
}

[GeneratedComClass]
internal partial class CoreWebView2BytesReceivedChangedHandler : ICoreWebView2BytesReceivedChangedEventHandler
{
    private readonly Action<ICoreWebView2DownloadOperation?> _callback;
    public CoreWebView2BytesReceivedChangedHandler(Action<ICoreWebView2DownloadOperation?> callback)
        => _callback = callback;
    public void Invoke(ICoreWebView2DownloadOperation? sender, nint args)
        => _callback(sender);
}