using Avalonia.Controls;

namespace WabbajackDownloader.Features.WebView;

internal sealed class JavaScriptRunner : IJavaScriptRunner, IAttachableProperty<NativeWebView>, IDisposable
{
    private NativeWebView WebView
        => _webView
        ?? throw new InvalidOperationException($"WebView is not attached or not available. Make sure the {nameof(JavaScriptRunner)} is bound to the WebView in XAML.");
    private NativeWebView? _webView;

    public void Attach(NativeWebView target)
    {
        _webView = target;
        _webView.WebMessageReceived += OnWebMessageReceived;
    }

    public void Detach() => Dispose();

    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;

    public Task<string?> ExecuteScriptAsync(string javascript)
        => WebView.InvokeScript(javascript);

    private void OnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        _ = WebView; // make sure webview is attached
        WebMessageReceived?.Invoke(this, e);
    }

    public void Dispose()
    {
        // The ViewModel holds a reference to this class, and this class holds a reference to the WebView,
        // which in turn holds a reference to the ViewModel through data binding (IJavaScriptRunner).
        // Setting it to null removes the circular reference.
        WebMessageReceived = null;
        _webView?.WebMessageReceived -= OnWebMessageReceived;
        _webView = null;
    }
}