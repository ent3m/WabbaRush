using Avalonia.Controls;

namespace WabbajackDownloader.Features.WebView;

/// <summary>
/// An abstraction over WebView2's JavaScript execution.
/// </summary>
internal interface IJavaScriptExecutionEngine : IAttachableProperty<NativeWebView>
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
}