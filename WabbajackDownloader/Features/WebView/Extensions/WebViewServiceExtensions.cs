using Microsoft.Extensions.DependencyInjection;

namespace WabbajackDownloader.Features.WebView;

internal static class WebViewServiceExtensions
{
    public static IServiceCollection AddWebView(this IServiceCollection services) => services
        .AddScoped<IJavaScriptRunner, JavaScriptRunner>();
}