using Microsoft.Extensions.DependencyInjection;

namespace WabbajackDownloader.Common.Serialization;

internal static class SerializerServiceExtensions
{
    public static IServiceCollection AddSerialization(this IServiceCollection services) => services
        .AddSingleton<SerializerOptions>();
}
