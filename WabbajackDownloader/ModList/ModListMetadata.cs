using System;
using System.Text.Json.Serialization;
using WabbajackDownloader.Extensions;

namespace WabbajackDownloader.ModList;

public class ModListMetadata
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("maintainers")]
    public string[] Maintainers { get; set; } = [];

    [JsonPropertyName("game")]
    public string Game { get; set; } = string.Empty;

    [JsonPropertyName("official")]
    public bool Official { get; set; }

    [JsonPropertyName("tags")]
    public string[] Tags { get; set; } = [];

    [JsonPropertyName("nsfw")]
    public bool NSFW { get; set; }

    [JsonPropertyName("utility_list")]
    public bool UtilityList { get; set; }

    [JsonPropertyName("image_contains_title")]
    public bool ImageContainsTitle { get; set; }

    [JsonPropertyName("DisplayVersionOnlyInInstallerView")]
    public bool DisplayVersionOnlyInInstallerView { get; set; }

    [JsonPropertyName("force_down")]
    public bool ForceDown { get; set; }

    [JsonPropertyName("links")]
    public Links Links { get; set; } = new();

    [JsonPropertyName("download_metadata")]
    public DownloadMetadata DownloadMetadata { get; set; } = new();

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("dateCreated")]
    public DateTime DateCreated { get; set; } = DateTime.UnixEpoch;

    [JsonPropertyName("dateUpdated")]
    public DateTime DateUpdated { get; set; }

    [JsonPropertyName("repositoryName")]
    public string RepositoryName { get; set; } = string.Empty;

    [JsonIgnore]
    public string Summary => summary ??= $"Author: {Author}\nVersion: {Version}\nSize: {DownloadMetadata?.TotalSize.DisplayByteSize()}";
    private string? summary;

    // Override ToString for text search to work in ComboBox
    public override string ToString() => Title;
}
