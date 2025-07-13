using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WabbajackDownloader.Configuration;
using WabbajackDownloader.ModList;

namespace WabbajackDownloader.Serializer;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(ModListMetadata[]))]
[JsonSerializable(typeof(Dictionary<string, Uri>))]
[JsonSerializable(typeof(FileDefinition))]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
