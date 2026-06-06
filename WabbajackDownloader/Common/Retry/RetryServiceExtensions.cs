using Microsoft.Extensions.DependencyInjection;

namespace WabbajackDownloader.Common.Retry;

internal static class RetryServiceExtensions
{
    public static IServiceCollection AddRetry(this IServiceCollection services) => services
        .AddTransient(typeof(RetryHandler<>), typeof(RetryHandler<>))
        .AddTransient<RetryOptions>(static s => s.GetRequiredService<AppSettings>().RetryOptions);
}