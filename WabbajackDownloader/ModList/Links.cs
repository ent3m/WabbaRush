using System.Text.Json.Serialization;

namespace WabbajackDownloader.ModList;

public class Links
{
    [JsonPropertyName("image")]
    public string ImageUri { get; set; } = string.Empty;
    [JsonPropertyName("readme")]
    public string Readme { get; set; } = string.Empty;
    [JsonPropertyName("download")]
    public string Download { get; set; } = string.Empty;
    [JsonPropertyName("machineURL")]
    public string MachineURL { get; set; } = string.Empty;
    [JsonPropertyName("discordURL")]
    public string DiscordURL { get; set; } = string.Empty;
    [JsonPropertyName("websiteURL")]
    public string WebsiteURL { get; set; } = string.Empty;
}