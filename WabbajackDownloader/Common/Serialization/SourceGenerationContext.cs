using System.Text.Json.Serialization;
using WabbajackDownloader.Common.Configuration;
using WabbajackDownloader.Features.WabbajackModList;
using WabbajackDownloader.Features.WabbajackRepo;

namespace WabbajackDownloader.Common.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(FileDefinition))]
[JsonSerializable(typeof(Dictionary<string, Uri>))]
[JsonSerializable(typeof(ModListMetadata[]))]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
