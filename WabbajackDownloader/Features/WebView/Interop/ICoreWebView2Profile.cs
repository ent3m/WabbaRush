using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WabbajackDownloader.Features.WebView.Interop;

[GeneratedComInterface]
[Guid("79110ad3-cd5d-4373-8bc3-c60658f17a5f")]
internal partial interface ICoreWebView2Profile
{
    void GetProfileName([MarshalAs(UnmanagedType.LPWStr)] out string name);
    void GetIsInPrivateModeEnabled([MarshalAs(UnmanagedType.Bool)] out bool enabled);
    void GetProfilePath([MarshalAs(UnmanagedType.LPWStr)] out string path);
    void GetDefaultDownloadFolderPath([MarshalAs(UnmanagedType.LPWStr)] out string path);
    void PutDefaultDownloadFolderPath([MarshalAs(UnmanagedType.LPWStr)] string path);
    void GetPreferredColorScheme(out WebViewPreferredColorScheme preferredColorScheme);
    void PutPreferredColorScheme(WebViewPreferredColorScheme preferredColorScheme);
}