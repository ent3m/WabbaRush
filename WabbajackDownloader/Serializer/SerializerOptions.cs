using System.Text.Json;

namespace WabbajackDownloader.Serializer;

internal static class SerializerOptions
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions()
        {
            TypeInfoResolver = SourceGenerationContext.Default,
            WriteIndented = true
        };
        options.Converters.Add(new HashJsonConverter());
        return options;
    }
}
