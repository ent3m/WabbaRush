using System.Text.Json.Serialization;
using WabbajackDownloader.Common.Retry;

namespace WabbajackDownloader.Common.Configuration;

internal sealed class AppSettings
{
    public string? DownloadFolder { get; set; }
    public string? WabbajackFile { get; set; }
    public string? SelectedModList { get; set; }
    public bool UseLocalWabbajackFile { get; set; } = false;
    public int MaxConcurrency { get; set; } = 3;
    public int MaxDownloadSize { get; set; } = 1200;
    public FileSizeUnit FileSizeUnit { get; set; } = FileSizeUnit.MB;
    public int TotalParts { get; set; } = 1;
    public int CurrentPart { get; set; } = 1;
    public int Timeout { get; set; } = 30;
    public bool VerifyDownloads { get; set; } = false;
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public string ModListFolder { get; set; } = "downloaded-modlists";
    public RetryOptions RetryOptions { get; set; } = new(MaxRetries: 3, BaseDelay: 2500, Multiplier: 2, Jitter: 500);
    public bool? IsDarkMode { get; set; } = null;
    [JsonIgnore] // Session-only setting
    public bool ClearDownloadFolder { get; set; } = false;
}