using System.IO;

namespace WabbajackDownloader.Common.Logging;

internal class FileLoggerProvider : ILoggerProvider, IDisposable
{
    private readonly StreamWriter _writer;
    private readonly LogLevelSwitch _logLevelSwitch;
    private readonly Lock _lock = new();

    public FileLoggerProvider(LogLevelSwitch logLevelSwitch)
    {
        var baseDir = AppContext.BaseDirectory;
        var path = Path.Combine(baseDir, "debug.json");
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        _logLevelSwitch = logLevelSwitch;
        _writer = new StreamWriter(fullPath, append: true)
        {
            AutoFlush = true
        };
    }

    public ILogger CreateLogger(string categoryName)
        => new FileLogger(categoryName, _writer, _lock, _logLevelSwitch);

    public void Dispose()
    {
        _writer.Dispose();
        GC.SuppressFinalize(this);
    }
}

file class FileLogger(string category, StreamWriter writer, Lock lockObject, LogLevelSwitch logLevelSwitch) : ILogger
{
    private readonly string _category = category;
    private readonly StreamWriter _writer = writer;
    private readonly Lock _lockObject = lockObject;
    private readonly LogLevelSwitch _logLevelSwitch = logLevelSwitch;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

    public bool IsEnabled(LogLevel level) => level >= _logLevelSwitch.MinLevel && level != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message))
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        lock (_lockObject)
        {
            _writer.WriteLine($"{timestamp} [{logLevel}] {_category}: {message}");
            if (exception is not null)
                _writer.WriteLine(exception);
        }
    }
}
