using WabbajackDownloader.Features.WebView;

namespace WabbajackDownloader.Features.Dashboard;

partial class DownloadPageViewModel
{
    private sealed class WebViewDownloadException(WebViewDownloadInterruptReason interruptReason, string fileName)
    : Exception($"Systemic download failure occured due to '{interruptReason}' when downloading '{fileName}'.")
    {
        public string FileName { get; } = fileName;
        public WebViewDownloadInterruptReason InterruptReason { get; } = interruptReason;
    }
}
