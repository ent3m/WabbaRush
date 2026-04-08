using Microsoft.Extensions.DependencyInjection;

namespace WabbajackDownloader.Common.Platform;

public static class PlatformServiceExtensions
{
    public static IServiceCollection AddPlatform(this IServiceCollection services) => services
        .AddSingleton<IStorageService, TopLevelService>()
        .AddSingleton<ILauncherService, TopLevelService>();
}