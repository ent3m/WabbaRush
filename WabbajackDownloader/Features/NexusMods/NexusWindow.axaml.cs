using Avalonia.Controls;
using Avalonia.Platform;

namespace WabbajackDownloader.Features.NexusMods;

public partial class NexusWindow : Window
{
    public NexusWindow()
    {
        InitializeComponent();
        if (webView.TryGetPlatformHandle() is IWindowsWebView2PlatformHandle handle)
        {
            nint coreWebView2 = handle.CoreWebView2;
            nint coreWebView2Controller = handle.CoreWebView2Controller;
        }
    }
}