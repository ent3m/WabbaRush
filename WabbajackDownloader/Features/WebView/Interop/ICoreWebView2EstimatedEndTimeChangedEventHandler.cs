using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WabbajackDownloader.Features.WebView.Interop;

[GeneratedComInterface]
[Guid("28f0d425-93fe-4e63-9f8d-2aeec6d3ba1e")]
internal partial interface ICoreWebView2EstimatedEndTimeChangedEventHandler
{
    // args is always null for this event
    void Invoke(ICoreWebView2DownloadOperation? sender, nint args);
}

[GeneratedComClass]
internal partial class EstimatedEndTimeChangedHandler : ICoreWebView2EstimatedEndTimeChangedEventHandler
{
    private readonly Action<ICoreWebView2DownloadOperation?> _callback;
    public EstimatedEndTimeChangedHandler(Action<ICoreWebView2DownloadOperation?> callback)
        => _callback = callback;
    public void Invoke(ICoreWebView2DownloadOperation? sender, nint args)
        => _callback(sender);
}