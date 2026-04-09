using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WabbajackDownloader.Features.WebView.Interop;

[GeneratedComInterface]
[Guid("e99bbe21-43e9-4544-a732-282764eafa60")]
internal partial interface ICoreWebView2DownloadStartingEventArgs
{
    void GetDownloadOperation(out ICoreWebView2DownloadOperation? operation);

    void GetCancel([MarshalAs(UnmanagedType.Bool)] out bool value);
    void PutCancel([MarshalAs(UnmanagedType.Bool)] bool value);

    void GetResultFilePath([MarshalAs(UnmanagedType.LPWStr)] out string value);
    void PutResultFilePath([MarshalAs(UnmanagedType.LPWStr)] string value);

    // BOOL is a 4-byte int in COM, not a 1-byte C# bool
    void GetHandled([MarshalAs(UnmanagedType.Bool)] out bool value);
    void PutHandled([MarshalAs(UnmanagedType.Bool)] bool value);

    void GetDeferral(out ICoreWebView2Deferral? deferral);
}

// For handling the download event asynchronously
[GeneratedComInterface]
[Guid("c10e7f7b-b585-46f0-a623-8befbf3e4ee0")]
internal partial interface ICoreWebView2Deferral
{
    void Complete();
}