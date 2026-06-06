using Microsoft.Extensions.DependencyInjection;
using Waypoint;

namespace WabbajackDownloader.Features.Frame;

internal static class FrameServiceExtensions
{
    public static IServiceCollection AddFrame(this IServiceCollection services) => services
        .AddTransient<MainWindow>()
        .AddTransient<MainWindowViewModel>();

    public static IViewRegistry RegisterFrame(this IViewRegistry views) => views
        .Register<MainWindow, MainWindowViewModel>();
}