namespace WabbajackDownloader.Common.Logging;

/// <summary>
/// Mutable log level holder. Defaults to <see cref="LogLevel.Information"/> until
/// user settings are loaded, at which point <see cref="MinLevel"/> is updated.
/// </summary>
internal sealed class LogLevelSwitch
{
    public LogLevel MinLevel { get; set; } = LogLevel.Information;
}
