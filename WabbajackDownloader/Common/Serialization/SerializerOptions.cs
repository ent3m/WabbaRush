using System.Text.Json;
using System.Text.Json.Serialization;
using WabbajackDownloader.Features.WabbajackRepo;

namespace WabbajackDownloader.Common.Serialization;

public sealed class SerializerOptions
{
    public JsonSerializerOptions Options { get; init; }

    public SerializerOptions()
    {
        var options = new JsonSerializerOptions()
        {
            TypeInfoResolver = SourceGenerationContext.Default,
            WriteIndented = true
        };
        options.Converters.Add(new HashJsonConverter());
        options.Converters.Add(new NexusCollectionLinkJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter<Game>());
        Options = options;
    }
}
