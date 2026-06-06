using Avalonia.Controls;

namespace WabbajackDownloader.Features.WebView;

/// <summary>
/// An abstraction over NativeWebView's JavaScript execution APIs.
/// </summary>
internal interface IJavaScriptRunner
{
    /// <summary>
    /// Runs JavaScript code from the <paramref name="javascript"/> parameter in the current top-level document rendered in the WebView.
    /// </summary>
    /// <param name="javascript">The JavaScript code to be run in the current top-level document rendered in the WebView.</param>
    /// <returns>A JSON encoded string that represents the result of running the provided JavaScript.</returns>
    /// <remarks>
    /// If the result is undefined, contains a reference cycle, or otherwise is not able to be encoded into JSON, the JSON null value is returned as the "null" string.
    /// A function that has no explicit return value returns undefined. If the script that was run throws an unhandled exception, then the result is also null.
    /// </remarks>
    Task<string?> ExecuteScriptAsync(string javascript);

    /// <summary>
    /// WebMessageReceived is raised when the <c>IsWebMessageEnabled</c> setting is set and the top-level document of the WebView 
    /// runs <c>window.chrome.webview.postMessage</c> or <c>window.chrome.webview.postMessageWithAdditionalObjects</c>.
    /// </summary>
    /// <remarks>
    /// The <c>postMessage</c> function is <c>void postMessage(object)</c> where object is any object supported by JSON conversion.
    /// When <c>postMessage</c> is called, the handler's Invoke method will be called with the <c>object</c> parameter <c>postMessage</c> converted to a JSON string.
    /// </remarks>
    event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
}