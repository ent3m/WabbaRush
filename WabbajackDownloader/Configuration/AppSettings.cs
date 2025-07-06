using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WabbajackDownloader.Core;
using Xilium.CefGlue;

namespace WabbajackDownloader.Configuration;

public class AppSettings
{
    public required string DownloadFolder { get; set; }
    public required string WabbajackFile { get; set; }
    public required int MaxConcurrentDownload { get; set; }
    public required int MaxRetries { get; set; }
    public required int MaxDownloadSize { get; set; }
    public required string NexusLandingPage { get; set; }
    public required bool DiscoverExistingFiles { get; set; }
    public required int BufferSize { get; set; }
    public required int MinRetryDelay { get; set; }
    public required int MaxRetryDelay { get; set; }
    public required bool CheckHash { get; set; }
    public required string UserAgent { get; set; }
    public required LogLevel LogLevel { get; set; }
    public required CefLogSeverity CefLogLevel { get; set; }

    private string filePath;

    // parameterless constructor for json deserializer
    public AppSettings() : this(string.Empty) { }

    private AppSettings(string filePath)
    {
        this.filePath = filePath;
    }

    public static AppSettings LoadOrGetDefaultSettings(string filePath) => LoadSettings(filePath) ?? GetDefaultSettings(filePath);

    public static AppSettings? LoadSettings(string filePath)
    {
        AppSettings? settings = default;
        try
        {
            var json = File.ReadAllText(filePath);
            settings = JsonSerializer.Deserialize<AppSettings>(json, SourceGenerationContext.Default.AppSettings);
            if (settings != null)
                settings.filePath = filePath;
        }
        catch (FileNotFoundException)
        {

        }
        return settings;
    }

    public void SaveSettings()
    {
        if (!string.IsNullOrEmpty(filePath))
        {
            var json = JsonSerializer.Serialize<AppSettings>(this, SourceGenerationContext.Default.AppSettings);
            File.WriteAllText(filePath, json);
        }
    }

    public static AppSettings GetDefaultSettings(string file = "") => new(file)
    {
        DownloadFolder = "",
        WabbajackFile = "",
        MaxConcurrentDownload = 3,
        MaxRetries = 3,
        MaxDownloadSize = 1000,
        NexusLandingPage = "https://www.nexusmods.com/skyrimspecialedition/mods/12604",
        DiscoverExistingFiles = true,
        BufferSize = 512 * 1024,
        MinRetryDelay = 1000,
        MaxRetryDelay = 3000,
        CheckHash = true,
        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0",
        LogLevel = LogLevel.Warning,
        CefLogLevel = CefLogSeverity.Error
    };
}
