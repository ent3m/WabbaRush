using System.Threading.Tasks;
using Xilium.CefGlue;
using Xilium.CefGlue.Common.Handlers;

namespace WabbajackDownloader.Cef;

/// <summary>
/// Set result when download is requested and make sure nothing is downloaded.
/// </summary>
internal class DownloadNothingHandler : DownloadHandler
{
    public TaskCompletionSource<string>? TaskCompletionSource { get; set; }

    protected override bool CanDownload(CefBrowser browser, string url, string requestMethod)
    {
        TaskCompletionSource?.SetResult(url);
        return false;
    }
}
