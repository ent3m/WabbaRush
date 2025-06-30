using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace WabbajackDownloader.Core;

public class AppSettings
{
    public required string DownloadFolder { get; set; }
    public required string WabbajackFile { get; set; }
    public required int MaxConcurrentDownload { get; set; }
    public required int MaxRetry { get; set; }
    public required int MaxDownloadSize { get; set; }
    public required string NexusLandingPage { get; set; }
    public required bool DiscoverExistingFiles { get; set; }
    public required int BufferSize { get; set; }
    public required int MinRetryDelay { get; set; }
    public required int MaxRetryDelay { get; set; }
    public required bool CheckHash { get; set; }
    public required LogLevel LogLevel { get; set; }
    public required CefLogSeverity CefLogLevel { get; set; }

    private readonly string file;

    private AppSettings(string file)
    {
        this.file = file;
    }

    public static AppSettings LoadSettings(string file)
    {
        AppSettings? settings = default;
        try
        {
            var json = File.ReadAllText(file);
            settings = JsonSerializer.Deserialize<AppSettings>(json, SourceGenerationContext.Default.AppSettings);
        }
        catch (Exception ex)
        {
            if (ex is not FileNotFoundException)
                App.Logger.LogCritical(ex.GetBaseException(), "Cannot load settings from {file}.", file);
        }
        return settings ?? GetDefaultSettings(file);
    }

    public void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize<AppSettings>(this, SourceGenerationContext.Default.AppSettings);
            File.WriteAllText(file, json);
        }
        catch (Exception ex)
        {
            App.Logger.LogError(ex.GetBaseException(), "Cannot save settings to file.");
            throw;
        }
    }

    private static AppSettings GetDefaultSettings(string file) => new(file)
    {
        DownloadFolder = "",
        WabbajackFile = "",
        MaxConcurrentDownload = 3,
        MaxRetry = 3,
        MaxDownloadSize = 1000,
        NexusLandingPage = "https://www.nexusmods.com/skyrimspecialedition/mods/12604",
        DiscoverExistingFiles = true,
        BufferSize = 1024 * 1024,
        MinRetryDelay = 1000,
        MaxRetryDelay = 3000,
        CheckHash = false,
        LogLevel = LogLevel.Information,
        CefLogLevel = CefLogSeverity.Error
    };
}
