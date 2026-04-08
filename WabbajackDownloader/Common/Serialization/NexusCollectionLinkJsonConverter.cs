using System.Text.Json;
using System.Text.Json.Serialization;
using WabbajackDownloader.Features.WabbajackRepo;

namespace WabbajackDownloader.Common.Serialization;

internal sealed class NexusCollectionLinkJsonConverter : JsonConverter<NexusCollectionLink>
{
    public override NexusCollectionLink Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new NexusCollectionLink();

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return result;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName");

            var propName = reader.GetString();
            reader.Read();

            switch (propName)
            {
                case "collectionId":
                    result.CollectionId = reader.TokenType switch
                    {
                        JsonTokenType.Number => reader.GetInt64().ToString(),
                        JsonTokenType.String => reader.GetString() ?? string.Empty,
                        _ => string.Empty
                    };
                    break;
                case "slug":
                    result.Slug = reader.GetString() ?? string.Empty;
                    break;
                case "domainName":
                    result.DomainName = reader.GetString() ?? string.Empty;
                    break;
                case "lastRevisionNumber":
                    result.LastRevisionNumber = reader.TokenType == JsonTokenType.Null ? null : reader.GetInt32();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, NexusCollectionLink value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("collectionId", value.CollectionId);
        writer.WriteString("slug", value.Slug);
        writer.WriteString("domainName", value.DomainName);
        if (value.LastRevisionNumber.HasValue)
            writer.WriteNumber("lastRevisionNumber", value.LastRevisionNumber.Value);
        else
            writer.WriteNull("lastRevisionNumber");
        writer.WriteEndObject();
    }
}
