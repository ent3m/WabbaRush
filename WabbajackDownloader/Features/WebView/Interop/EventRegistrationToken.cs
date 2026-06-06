using System.Runtime.InteropServices;

namespace WabbajackDownloader.Features.WebView.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct EventRegistrationToken
{
    public long Value;
}