using System.Text.Json.Serialization;

namespace WabbajackDownloader.Features.WabbajackRepo;

public sealed class NexusCollectionLink
{
    [JsonPropertyName("collectionId")]
    public string CollectionId { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    // "skyrimspecialedition" etc
    [JsonPropertyName("domainName")]
    public string DomainName { get; set; } = string.Empty;

    [JsonPropertyName("lastRevisionNumber")]
    public int? LastRevisionNumber { get; set; }
}
