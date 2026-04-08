using Microsoft.Extensions.DependencyInjection;

namespace WabbajackDownloader.Common.Logging;

public static class LoggingServiceExtensions
{
    public static IServiceCollection AddLogger(this IServiceCollection services)
    {
        services.AddSingleton<LogLevelSwitch>();
#if DEBUG
        services.AddSingleton<ILoggerProvider, DebugLoggerProvider>();
#else
        services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
#endif
        return services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));
    }
}
