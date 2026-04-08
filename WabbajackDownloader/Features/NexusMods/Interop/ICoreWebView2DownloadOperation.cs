using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WabbajackDownloader.Features.NexusMods.Interop;

[GeneratedComInterface]
[Guid("3d6b6cf2-afe1-44c7-a995-c65117714336")]
internal partial interface ICoreWebView2DownloadOperation
{
    // Events for monitoring progress
    void AddBytesReceivedChanged(
        ICoreWebView2BytesReceivedChangedEventHandler handler,
        out EventRegistrationToken token);
    void RemoveBytesReceivedChanged(EventRegistrationToken token);

    void AddEstimatedEndTimeChanged(
        nint handler,   // stub — we don't need this event
        out EventRegistrationToken token);
    void RemoveEstimatedEndTimeChanged(EventRegistrationToken token);

    void AddStateChanged(
        ICoreWebView2StateChangedEventHandler handler,
        out EventRegistrationToken token);
    void RemoveStateChanged(EventRegistrationToken token);

    void GetUri([MarshalAs(UnmanagedType.LPWStr)] out string value);
    void GetContentDisposition([MarshalAs(UnmanagedType.LPWStr)] out string value);
    void GetMimeType([MarshalAs(UnmanagedType.LPWStr)] out string value);
    void GetTotalBytesToReceive(out long value);     // may be -1 if unknown
    void GetBytesReceived(out long value);
    void GetEstimatedEndTime([MarshalAs(UnmanagedType.LPWStr)] out string value);
    void GetResultFilePath([MarshalAs(UnmanagedType.LPWStr)] out string value);
    void GetState(out CoreWebView2DownloadState value);
    void GetInterruptReason(out CoreWebView2DownloadInterruptReason value);
    void Cancel();
    void Pause();
    void Resume();
    void GetCanResume([MarshalAs(UnmanagedType.Bool)] out bool value);
}