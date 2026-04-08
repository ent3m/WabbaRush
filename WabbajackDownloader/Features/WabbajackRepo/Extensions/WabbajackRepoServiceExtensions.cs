using Microsoft.Extensions.DependencyInjection;

namespace WabbajackDownloader.Features.WabbajackRepo;

internal static class WabbajackRepoServiceExtensions
{
    public static IServiceCollection AddWabbajackRepo(this IServiceCollection services) => services
        .AddSingleton<RepositoriesDownloader>();
}
