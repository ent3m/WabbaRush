using Microsoft.Extensions.DependencyInjection;
using Waypoint;

namespace WabbajackDownloader.Features.NexusMods;

internal static class NexusModsServiceExtensions
{
    public static IServiceCollection AddNexusMods(this IServiceCollection services) => services
        .AddTransient<NexusWindow>()
        .AddTransient<NexusWindowViewModel>();

    public static IViewRegistry RegisterNexusMods(this IViewRegistry views) => views
        .Register<NexusWindow, NexusWindowViewModel>();
}
