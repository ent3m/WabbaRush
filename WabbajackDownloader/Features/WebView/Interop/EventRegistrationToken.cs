using System.Runtime.InteropServices;

namespace WabbajackDownloader.Features.WebView.Interop;

// EventRegistrationToken is a value type used by all add_/remove_ event pairs
[StructLayout(LayoutKind.Sequential)]
internal struct EventRegistrationToken
{
    public long Value;
}