using System.IO;
using System.Text.Json;
using WabbajackDownloader.Common.Serialization;

namespace WabbajackDownloader.Common.Configuration;

internal sealed class SettingsManager(ILogger<SettingsManager> logger)
{
    private string _filePath = Path.Combine(AppContext.BaseDirectory, "settings.json");
    private readonly Lock _lock = new();
    private readonly ILogger<SettingsManager> _logger = logger;

    public AppSettings Settings { get; private set; } = new();

    public SettingsManager(string filePath, ILogger<SettingsManager> logger) : this(logger)
    {
        _filePath = filePath;
    }

    public void Load() => Load(_filePath);

    public void Load(string filePath)
    {
        lock (_lock)
        {
            var prevPath = _filePath;
            try
            {
                // Load settings and update file path
                var json = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, SourceGenerationContext.Default.AppSettings);

                if (settings != null)
                {
                    ValidateSettings(settings);
                    Settings = settings;
                    _filePath = filePath;
                }
                _logger.LogDebug("Settings loaded from {FilePath}", _filePath);
            }
            catch (Exception ex)
            {
                // Revert to default settings and previous path if loading fails
                Settings = new();
                _filePath = prevPath;
                _logger.LogWarning(ex, "Failed to load settings from {FilePath}. Using defaults.", filePath);
            }
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = JsonSerializer.Serialize<AppSettings>(Settings, SourceGenerationContext.Default.AppSettings);
                File.WriteAllText(_filePath, json);
                _logger.LogDebug("Settings saved to {FilePath}", _filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save settings to {FilePath}", _filePath);
            }
        }
    }

    #region Validation
    public const int DownloadSizeLowerBound = 1;
    public const int DownloadSizeUpperBound = 2000;
    public const int ConcurrencyLowerBound = 1;
    public const int ConcurrencyUpperBound = 10;
    public const int TimeoutLowerBound = 10;
    public const int TimeoutUpperBound = 120;
    public const int RetriesLowerBound = 0;
    public const int RetriesUpperBound = 10;
    public const int TotalPartsLowerBound = 1;
    public const int TotalPartsUpperBound = 10;
    private static void ValidateSettings(AppSettings settings)
    {
        settings.MaxDownloadSize = Math.Clamp(settings.MaxDownloadSize, DownloadSizeLowerBound, DownloadSizeUpperBound);
        settings.MaxConcurrency = Math.Clamp(settings.MaxConcurrency, ConcurrencyLowerBound, ConcurrencyUpperBound);
        settings.Timeout = Math.Clamp(settings.Timeout, TimeoutLowerBound, TimeoutUpperBound);
        settings.TotalParts = Math.Clamp(settings.TotalParts, TotalPartsLowerBound, TotalPartsUpperBound);
        settings.CurrentPart = Math.Clamp(settings.CurrentPart, TotalPartsLowerBound, settings.TotalParts);
        settings.RetryOptions = settings.RetryOptions with
        {
            MaxRetries = Math.Clamp(settings.RetryOptions.MaxRetries, RetriesLowerBound, RetriesUpperBound)
        };
        if (!File.Exists(settings.WabbajackFile))
            settings.WabbajackFile = null;
        if (!Directory.Exists(settings.DownloadFolder))
            settings.DownloadFolder = null;
    }
    #endregion
}
