using System.Text.Json.Serialization;
using WabbajackDownloader.Common.Progress;

namespace WabbajackDownloader.Features.WabbajackRepo;

// Adapted from https://github.com/wabbajack-tools/wabbajack/blob/main/Wabbajack.DTOs/ModList/ModListMetadata.cs
public class ModListMetadata
{
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

    [JsonPropertyName("author")] public string Author { get; set; } = string.Empty;

    [JsonPropertyName("maintainers")] public string[] Maintainers { get; set; } = [];

    [JsonPropertyName("game")] public Game Game { get; set; }

    [JsonPropertyName("official")] public bool Official { get; set; }

    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];

    [JsonPropertyName("nsfw")] public bool NSFW { get; set; }

    [JsonPropertyName("utility_list")] public bool UtilityList { get; set; }

    [JsonPropertyName("image_contains_title")] public bool ImageContainsTitle { get; set; }

    [JsonPropertyName("DisplayVersionOnlyInInstallerView")] public bool DisplayVersionOnlyInInstallerView { get; set; }

    [JsonPropertyName("force_down")] public bool ForceDown { get; set; }

    [JsonPropertyName("links")] public Links Links { get; set; } = new();

    [JsonPropertyName("download_metadata")] public DownloadMetadata? DownloadMetadata { get; set; }

    [JsonPropertyName("version")] public Version? Version { get; set; }

    [JsonIgnore] public ModListSummary ValidationSummary { get; set; } = new();

    [JsonPropertyName("dateCreated")] public DateTime DateCreated { get; set; } = DateTime.UnixEpoch;

    [JsonPropertyName("dateUpdated")] public DateTime DateUpdated { get; set; }

    [JsonPropertyName("repositoryName")] public string RepositoryName { get; set; } = string.Empty;
    [JsonIgnore] public string NamespacedName => $"{RepositoryName}/{Links.MachineURL}";

    // Summary text for Tooltip in ComboBox item
    [JsonIgnore]
    public string Summary => summary ??=
        $"Author: {Author}\nVersion: {Version}\nSize: {DownloadMetadata?.Size.DisplayByteSize()} + {DownloadMetadata?.SizeOfArchives.DisplayByteSize()}";
    private string? summary;

    // Override ToString for text search to work in ComboBox
    public override string ToString() => Title;
}
