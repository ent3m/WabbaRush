using Microsoft.Extensions.DependencyInjection;
using Waypoint;

namespace WabbajackDownloader.Features.Splashscreen;

internal static class SplashscreenServiceExtensions
{
    public static IServiceCollection AddSplashscreen(this IServiceCollection services) => services
        .AddTransient<Splashscreen>()
        .AddTransient<SplashscreenViewModel>();

    public static IViewRegistry RegisterSplashscreen(this IViewRegistry views) => views
        .Register<Splashscreen, SplashscreenViewModel>();
}