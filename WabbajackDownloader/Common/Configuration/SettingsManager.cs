using System.IO;
using System.Text.Json;
using WabbajackDownloader.Common.Serialization;

namespace WabbajackDownloader.Common.Configuration;

public class SettingsManager(SerializerOptions options, ILogger<SettingsManager> logger)
{
    public AppSettings Settings { get; private set; } = new();

    private string _filePath = Path.Combine(AppContext.BaseDirectory, "settings.json");
    private readonly Lock _lock = new();
    private readonly JsonSerializerOptions _options = options.Options;
    private readonly ILogger<SettingsManager> _logger = logger;

    public SettingsManager(string filePath, SerializerOptions options, ILogger<SettingsManager> logger) : this(options, logger)
    {
        _filePath = filePath;
    }

    public void Load() => Load(_filePath);

    public void Load(string filePath)
    {
        var prevPath = _filePath;
        try
        {
            // Load settings and update file path
            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _options);
            if (settings != null)
            {
                ClampSettings(settings);
                Settings = settings;
                _filePath = filePath;
            }
            _logger.LogInformation("Settings loaded from {FilePath}", _filePath);
        }
        catch (Exception ex)
        {
            // Revert to default settings and previous path if loading fails
            Settings = new();
            _filePath = prevPath;
            _logger.LogWarning(ex, "Failed to load settings from {FilePath}. Using defaults.", filePath);
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = JsonSerializer.Serialize<AppSettings>(Settings, _options);
                File.WriteAllText(_filePath, json);
                _logger.LogInformation("Settings saved to {FilePath}", _filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings to {FilePath}", _filePath);
            }
        }
    }

    #region Clamps
    public const double DownloadSizeLowerBoundDouble = 1;
    public const double DownloadSizeUpperBoundDouble = 9999;
    public const int DownloadSizeLowerBound = 1;
    public const int DownloadSizeUpperBound = 9999;
    public const int ConcurrencyLowerBound = 1;
    public const int ConcurrencyUpperBound = 10;
    public const int TimeoutLowerBound = 1;
    public const int TimeoutUpperBound = 300;
    public const int RetriesLowerBound = 0;
    public const int RetriesUpperBound = 10;
    public const int BufferSizeLowerBound = 4 * 1024;
    public const int BufferSizeUpperBound = 16 * 1024 * 1024;
    private static void ClampSettings(AppSettings settings)
    {
        settings.MaxDownloadSizeMB = Math.Clamp(settings.MaxDownloadSizeMB, DownloadSizeLowerBound, DownloadSizeUpperBound);
        settings.MaxConcurrency = Math.Clamp(settings.MaxConcurrency, ConcurrencyLowerBound, ConcurrencyUpperBound);
        settings.Timeout = Math.Clamp(settings.Timeout, TimeoutLowerBound, TimeoutUpperBound);
        settings.BufferSize = Math.Clamp(settings.BufferSize, BufferSizeLowerBound, BufferSizeUpperBound);
        settings.RetryOptions = settings.RetryOptions with
        {
            Retries = Math.Clamp(settings.RetryOptions.Retries, RetriesLowerBound, RetriesUpperBound)
        };
    }
    #endregion
}
