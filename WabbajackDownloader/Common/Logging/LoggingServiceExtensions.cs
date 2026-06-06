using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WabbajackDownloader.Common.Logging;

internal static class LoggingServiceExtensions
{
    private static ILoggingBuilder AddPlatformProvider(this ILoggingBuilder builder)
    {
#if DEBUG
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DebugLoggerProvider>());
#else
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());
#endif
        return builder;
    }

    public static IServiceCollection AddLogger(this IServiceCollection services) => services
        .AddSingleton<LogLevelSwitch>()
        .AddLogging(static builder => builder
        .SetMinimumLevel(LogLevel.Trace)
        .AddPlatformProvider());
}