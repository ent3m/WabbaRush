using Microsoft.Extensions.DependencyInjection;
using Waypoint;

namespace WabbajackDownloader.Features.Dashboard;

internal static class DashboardServiceExtensions
{
    public static IServiceCollection AddDashboard(this IServiceCollection services) => services
        .AddTransient<Splashscreen>()
        .AddTransient<SplashscreenViewModel>()
        .AddTransient<MainWindow>()
        .AddTransient<MainWindowViewModel>()
        .AddTransient<NexusWindow>()
        .AddTransient<NexusWindowViewModel>();

    public static IViewRegistry RegisterDashboard(this IViewRegistry views) => views
        .Register<Splashscreen, SplashscreenViewModel>()
        .Register<MainWindow, MainWindowViewModel>()
        .Register<NexusWindow, NexusWindowViewModel>();
}
