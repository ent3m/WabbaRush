using Microsoft.Extensions.DependencyInjection;
using WabbajackDownloader.Common.Logging;
using WabbajackDownloader.Common.Serialization;

namespace WabbajackDownloader.Common.Configuration;

public static class ConfigurationServiceExtensions
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services) => services
        .AddSingleton<SettingsManager>(sp =>
        {
            var options = sp.GetRequiredService<SerializerOptions>();
            var logger = sp.GetRequiredService<ILogger<SettingsManager>>();
            var manager = new SettingsManager(options, logger);
            manager.Load();

            // Sync the file logger's level to the user-configured value now that settings are loaded.
            var logLevelSwitch = sp.GetRequiredService<LogLevelSwitch>();
            logLevelSwitch.MinLevel = manager.Settings.LogLevel;

            return manager;
        })
        .AddTransient<AppSettings>(sp => sp.GetRequiredService<SettingsManager>().Settings);
}
