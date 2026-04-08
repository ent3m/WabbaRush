using Microsoft.Extensions.DependencyInjection;
using Waypoint;

namespace WabbajackDownloader.Features.Dashboard;

internal static class DashboardServiceExtensions
{
    public static IServiceCollection AddDashboard(this IServiceCollection services) => services
        .AddTransient<MainWindow>()
        .AddTransient<MainWindowViewModel>()
        .AddTransient<Splashscreen>()
        .AddTransient<SplashscreenViewModel>();

    public static IViewRegistry RegisterDashboard(this IViewRegistry views) => views
        .Register<Splashscreen, SplashscreenViewModel>()
        .Register<MainWindow, MainWindowViewModel>();
}
