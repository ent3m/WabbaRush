using Avalonia.Controls;
using Avalonia.Platform;

namespace WabbajackDownloader.Features.Dashboard;

internal sealed partial class DownloadPage : UserControl
{
    public DownloadPage()
    {
        InitializeComponent();

        WebView.EnvironmentRequested += OnEnvironmentRequested;
    }

    // Force "en-US" locale for WebView2 for download button script to work
    private void OnEnvironmentRequested(object? sender, WebViewEnvironmentRequestedEventArgs args)
    {
        if (args is WindowsWebView2EnvironmentRequestedEventArgs webView2Args)
            webView2Args.Language = "en-US";

        WebView.EnvironmentRequested -= OnEnvironmentRequested;
    }
}
