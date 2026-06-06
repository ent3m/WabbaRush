using Microsoft.Extensions.DependencyInjection;

namespace WabbajackDownloader.Common.Platform;

internal static class PlatformServiceExtensions
{
    public static IServiceCollection AddPlatform(this IServiceCollection services) => services
        .AddSingleton<IStorageService, TopLevelService>()
        .AddSingleton<ILauncherService, TopLevelService>();
}