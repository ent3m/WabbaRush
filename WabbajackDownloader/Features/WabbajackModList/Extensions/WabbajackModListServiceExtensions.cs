using Microsoft.Extensions.DependencyInjection;

namespace WabbajackDownloader.Features.WabbajackModList.Services;

internal static class WabbajackModListServiceExtensions
{
    public static IServiceCollection AddWabbajackModList(this IServiceCollection services) => services
        .AddTransient<ModListDownloader>()
        .AddTransient<ModlistExtractor>();
}
