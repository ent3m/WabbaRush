using Microsoft.Extensions.DependencyInjection;
using WabbajackDownloader.Common.Configuration;

namespace WabbajackDownloader.Common.Retry;

public static class RetryServiceExtensions
{
    public static IServiceCollection AddRetry(this IServiceCollection services) => services
        .AddTransient(typeof(RetryHandler<>), typeof(RetryHandler<>))
        .AddTransient<RetryOptions>(sp => sp.GetRequiredService<AppSettings>().RetryOptions);
}
