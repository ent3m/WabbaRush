using Microsoft.Extensions.DependencyInjection;
using Waypoint;

namespace WabbajackDownloader.Features.Dashboard;

internal static class DashboardServiceExtensions
{
    public static IServiceCollection AddDashboard(this IServiceCollection services) => services
        .AddTransient<SetupPage>()
        .AddTransient<SetupPageViewModel>()
        .AddTransient<SettingsPage>()
        .AddTransient<SettingsPageViewModel>()
        .AddTransient<DownloadPage>()
        .AddTransient<DownloadPageViewModel>();

    public static IViewRegistry RegisterDashboard(this IViewRegistry views) => views
        .Register<SetupPage, SetupPageViewModel>()
        .Register<SettingsPage, SettingsPageViewModel>()
        .Register<DownloadPage, DownloadPageViewModel>();
}
