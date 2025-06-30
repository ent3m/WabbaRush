using System.Text.Json.Serialization;

namespace WabbajackDownloader.Core;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
