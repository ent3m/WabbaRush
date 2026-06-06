using Microsoft.Extensions.DependencyInjection;
using WabbajackDownloader.Common.Logging;

namespace WabbajackDownloader.Common.Configuration;

internal static class ConfigurationServiceExtensions
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services) => services
        .AddSingleton<SettingsManager>(static sp =>
        {
            // Load settings using the default path
            var logger = sp.GetRequiredService<ILogger<SettingsManager>>();
            var manager = new SettingsManager(logger);
            manager.Load();

            // Sync the file logger's level to the user-configured value now that settings are loaded.
            var logLevelSwitch = sp.GetRequiredService<LogLevelSwitch>();
            logLevelSwitch.MinLevel = manager.Settings.LogLevel;

            return manager;
        })
        // Resolve the AppSettings from SettingsManager so that it's always fresh
        .AddTransient<AppSettings>(static sp => sp.GetRequiredService<SettingsManager>().Settings);
}
