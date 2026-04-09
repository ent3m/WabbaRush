using Avalonia.Controls;

namespace WabbajackDownloader.Features.WebView;

internal class JavaScriptExecutionEngine : IJavaScriptExecutionEngine, IDisposable
{
    private NativeWebView? _webView;

    public void Attach(NativeWebView target) =>
        _webView = target;

    public void Detach() => Dispose();

    public Task<string?> ExecuteScriptAsync(string javascript)
    {
        if (_webView is not null)
            return _webView.InvokeScript(javascript);

        throw new InvalidOperationException("WebView is not attached or not available. Make sure this JavaScriptExecutionEngine is bound to the WebView in XAML.");
    }

    public void Dispose()
    {
        // The ViewModel holds a reference to this class, and this class holds a reference to the WebView,
        // which in turn holds a reference to the ViewModel through data binding.
        // Setting it to null removes the circular reference.
        _webView = null;
    }
}