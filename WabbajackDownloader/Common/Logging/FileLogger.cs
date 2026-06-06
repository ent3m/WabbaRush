using System.IO;
using System.Threading.Channels;

namespace WabbajackDownloader.Common.Logging;

internal sealed class FileLoggerProvider : ILoggerProvider, IDisposable
{
    private readonly StreamWriter _writer;
    private readonly LogLevelSwitch _logLevelSwitch;
    private readonly Channel<string> _channel;
    private readonly Task _backgroundTask;

    public FileLoggerProvider(LogLevelSwitch logLevelSwitch)
    {
        var baseDir = AppContext.BaseDirectory;
        var timeStamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        var path = Path.Combine(baseDir, $"log-{timeStamp}.txt");
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        _logLevelSwitch = logLevelSwitch;
        _writer = new StreamWriter(fullPath) { AutoFlush = true };

        _channel = Channel.CreateUnbounded<string>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });

        _backgroundTask = Task.Run(ProcessLogQueueAsync);
    }

    private async Task ProcessLogQueueAsync()
    {
        try
        {
            await foreach (var message in _channel.Reader.ReadAllAsync())
            {
                _writer.WriteLine(message);
            }
        }
        catch (OperationCanceledException)
        {

        }
        finally
        {
            // Flush any remaining items before closing
            while (_channel.Reader.TryRead(out var message))
            {
                _writer.WriteLine(message);
            }
        }
    }

    public ILogger CreateLogger(string categoryName)
        => new FileLogger(categoryName, _channel.Writer, _logLevelSwitch);

    public void Dispose()
    {
        _channel.Writer.Complete();
        try { _backgroundTask.GetAwaiter().GetResult(); } catch { }
        _writer.Dispose();
    }
}

file sealed class FileLogger(string category, ChannelWriter<string> channelWriter, LogLevelSwitch logLevelSwitch) : ILogger
{
    private readonly string _category = category;
    private readonly ChannelWriter<string> _channelWriter = channelWriter;
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
        string logLine;
        if (exception is null)
            logLine = $"{timestamp} [{logLevel}] {_category}: {message}";
        else
            logLine = $"{timestamp} [{logLevel}] {_category}: {message}{Environment.NewLine}{exception}";

        _channelWriter.TryWrite(logLine);
    }
}
