using WabbajackDownloader.Common.Retry;

namespace WabbajackDownloader.Common.Configuration;

public class AppSettings
{
    public string? DownloadFolder { get; set; }
    public string? WabbajackFile { get; set; }
    public string? SelectedModList { get; set; }
    public bool UseLocalWabbajackFile { get; set; } = false;
    public bool SkipFailedModDownloads { get; set; } = false;
    public int MaxConcurrency { get; set; } = 3;
    public int MaxDownloadSizeMB { get; set; } = 2000;
    public int BufferSize { get; set; } = 512 * 1024;
    public int Timeout { get; set; } = 30;
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public string ModListFolder { get; set; } = "downloaded-modlists";
    public RetryOptions RetryOptions { get; set; } = new(Retries: 3, Delay: 500, Multiplier: 2, Jitter: 1000);
}
