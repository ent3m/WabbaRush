using System.Text.Json.Serialization;

namespace WabbajackDownloader.Configuration;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
