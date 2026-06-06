using System.Text.Json.Serialization;

namespace WabbajackDownloader.Common.Update;

internal record GitHubRelease([property: JsonPropertyName("tag_name")] string TagName, [property: JsonPropertyName("html_url")] string HtmlUrl);
