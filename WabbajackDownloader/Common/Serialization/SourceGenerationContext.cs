using System.Text.Json.Serialization;
using WabbajackDownloader.Common.Update;
using WabbajackDownloader.Features.WabbajackModList;
using WabbajackDownloader.Features.WabbajackRepo;

namespace WabbajackDownloader.Common.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true,
    Converters = [typeof(JsonStringEnumConverter<Game>), typeof(NexusCollectionLinkJsonConverter), typeof(HashJsonConverter)])]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(FileDefinition))]
[JsonSerializable(typeof(Dictionary<string, Uri>))]
[JsonSerializable(typeof(ModListMetadata[]))]
[JsonSerializable(typeof(GitHubRelease))]
internal sealed partial class SourceGenerationContext : JsonSerializerContext
{

}