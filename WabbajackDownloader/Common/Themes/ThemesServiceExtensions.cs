using Microsoft.Extensions.DependencyInjection;

namespace WabbajackDownloader.Common.Themes;

internal static class ThemesServiceExtensions
{
    public static IServiceCollection AddThemes(this IServiceCollection services) => services
        .AddSingleton<ThemeManager>();
}
