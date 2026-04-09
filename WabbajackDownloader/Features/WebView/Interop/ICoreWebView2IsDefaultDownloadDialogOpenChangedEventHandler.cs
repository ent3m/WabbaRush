using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WabbajackDownloader.Features.WebView.Interop;

[GeneratedComInterface]
[Guid("3117da26-ae13-438d-bd46-edbeb2c4ce81")]
internal partial interface ICoreWebView2IsDefaultDownloadDialogOpenChangedEventHandler
{
    // args is always null for this event
    void Invoke(ICoreWebView2? sender, nint args);
}

[GeneratedComClass]
internal partial class CoreWebView2IsDefaultDownloadDialogOpenChangedEventHandler : ICoreWebView2IsDefaultDownloadDialogOpenChangedEventHandler
{
    private readonly Action<ICoreWebView2?> _callback;
    public CoreWebView2IsDefaultDownloadDialogOpenChangedEventHandler(Action<ICoreWebView2?> callback)
        => _callback = callback;
    public void Invoke(ICoreWebView2? sender, nint args)
        => _callback(sender);
}