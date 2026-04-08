namespace WabbajackDownloader.Features.NexusMods;

/// <summary>
/// Acts as a router for the <see cref="Avalonia.Controls.NativeWebView"/>, allowing the ViewModel to call methods without needing to know the control directly.
/// </summary>
/// <remarks>
/// The <see cref="WebViewController"/> holds a delegate that points to the <see cref="Avalonia.Controls.NativeWebView.InvokeScript(string)"/> method.
/// The delegate is assigned when the <see cref="WebViewController"/> is assigned as an <see cref="Avalonia.AttachedProperty{TValue}"/> on the <see cref="Avalonia.Controls.NativeWebView"/>.
/// By calling <see cref="InvokeScriptAsync"/>, the ViewModel invokes the delegate, which in turn calls the <see cref="Avalonia.Controls.NativeWebView.InvokeScript(string)"/> method.
/// <para>The ViewModel needs an Observable Property of type <see cref="WebViewController"/> and binds it to the Attached Property in XAML.</para>
/// </remarks>
public class WebViewController
{
    // Points to the InvokeScript method. Assigned in WebViewExtensions.
    private Func<string, Task<string?>>? _invokeScriptDelegate;
    internal void Attach(Func<string, Task<string?>> invokeScriptAction)
        => _invokeScriptDelegate = invokeScriptAction;

    // Called by the ViewModel to execute JavaScript in the attached NativeWebView
    public Task<string?> InvokeScriptAsync(string script) =>
        _invokeScriptDelegate is not null ? _invokeScriptDelegate(script)
        : throw new InvalidOperationException("WebView is not attached to the controller.");
}