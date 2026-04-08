using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WabbajackDownloader.Features.NexusMods.Interop;

[GeneratedComInterface]
[Guid("e99bbe21-43e9-4544-a732-282764eafa60")]
internal partial interface ICoreWebView2DownloadStartingEventArgs
{
    // Vtable order must exactly match the COM IDL. Verify in WebView2.h.
    // get_DownloadOperation
    void GetDownloadOperation(out ICoreWebView2DownloadOperation? operation);

    // get_Cancel / put_Cancel
    void GetCancel([MarshalAs(UnmanagedType.Bool)] out bool value);
    void PutCancel([MarshalAs(UnmanagedType.Bool)] bool value);

    // get_ResultFilePath / put_ResultFilePath
    void GetResultFilePath([MarshalAs(UnmanagedType.LPWStr)] out string value);
    void PutResultFilePath([MarshalAs(UnmanagedType.LPWStr)] string value);

    // get_Handled / put_Handled — BOOL is a 4-byte int in COM, not a 1-byte C# bool
    void GetHandled([MarshalAs(UnmanagedType.Bool)] out bool value);
    void PutHandled([MarshalAs(UnmanagedType.Bool)] bool value);

    // GetDeferral
    void GetDeferral(out ICoreWebView2Deferral? deferral);
}

// For handling the download event asynchronously
[GeneratedComInterface]
[Guid("c10e7f7b-b585-46f0-a623-8befbf3e4ee0")]
internal partial interface ICoreWebView2Deferral
{
    void Complete();
}