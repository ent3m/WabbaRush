using Microsoft.Extensions.DependencyInjection;

namespace WabbajackDownloader.Common.Update;

internal static class UpdateServiceExtensions
{
    public static IServiceCollection AddUpdate(this IServiceCollection services) => services
        .AddSingleton<UpdateHandler>();
}