using Microsoft.Extensions.DependencyInjection;

namespace WabbajackDownloader.Features.WebView;

internal static class WebViewServiceExtensions
{
    public static IServiceCollection AddNexusMods(this IServiceCollection services) => services
        .AddTransient<IJavaScriptExecutionEngine, JavaScriptExecutionEngine>();
}