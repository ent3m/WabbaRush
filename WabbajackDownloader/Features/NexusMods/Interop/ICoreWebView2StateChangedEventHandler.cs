using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WabbajackDownloader.Features.NexusMods.Interop;

[GeneratedComInterface]
[Guid("81336594-7ede-4ba9-bf71-acf0a95b58dd")]
internal partial interface ICoreWebView2StateChangedEventHandler
{
    // args is always null for this event
    void Invoke(ICoreWebView2DownloadOperation? sender, nint args);
}

[GeneratedComClass]
internal partial class StateChangedHandler : ICoreWebView2StateChangedEventHandler
{
    private readonly Action<ICoreWebView2DownloadOperation?> _callback;
    public StateChangedHandler(Action<ICoreWebView2DownloadOperation?> callback)
        => _callback = callback;
    public void Invoke(ICoreWebView2DownloadOperation? sender, nint args)
        => _callback(sender);
}