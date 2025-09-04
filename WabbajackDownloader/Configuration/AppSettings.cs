using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using WabbajackDownloader.Serializer;
using Xilium.CefGlue;

namespace WabbajackDownloader.Configuration;

public class AppSettings
{
    public string? DownloadFolder { get; set; }
    public string? WabbajackFile { get; set; }
    public string? SelectedModList { get; set; }
    public bool UseLocalFile { get; set; } = false;
    public int MaxConcurrency { get; set; } = 3;
    public int MaxRetries { get; set; } = 3;
    public int MaxDownloadSize { get; set; } = 2000;
    public string NexusLandingPage { get; set; } = "https://www.nexusmods.com/skyrimspecialedition/mods/12604";
    public bool DiscoverExistingFiles { get; set; } = true;
    public int BufferSize { get; set; } = 512 * 1024;
    public int RetryDelay { get; set; } = 500;
    public int DelayMultiplier { get; set; } = 2;
    public int DelayJitter { get; set; } = 1000;
    public int HttpTimeout { get; set; } = 30;
    public bool CheckHash { get; set; } = true;
    public bool AppendDebugLog { get; set; } = false;
    public string ModListDownloadPath { get; set; } = "downloaded-modlists";
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public CefLogSeverity CefLogLevel { get; set; } = CefLogSeverity.Warning;

    private string? filePath;

    // parameterless constructor for json deserializer
    public AppSettings() { }

    private AppSettings(string? filePath)
    {
        this.filePath = filePath;
    }

    public static AppSettings LoadOrGetDefaultSettings(string filePath) => LoadSettings(filePath) ?? new(filePath);

    public static AppSettings? LoadSettings(string filePath)
    {
        AppSettings? settings = default;
        try
        {
            var json = File.ReadAllText(filePath);
            settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions.Options);
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
            var json = JsonSerializer.Serialize<AppSettings>(this, SerializerOptions.Options);
            File.WriteAllText(filePath, json);
        }
    }
}
