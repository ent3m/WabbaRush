using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;

namespace WabbajackDownloader.Logging;

internal class FileLoggerProvider : ILoggerProvider
{
    private readonly StreamWriter writer;
    private readonly LogLevel minLevel;
    private readonly Lock lockObject = new();

    public FileLoggerProvider(string filePath, LogLevel minLevel, bool append)
    {
        this.minLevel = minLevel;
        var fullPath = Path.GetFullPath(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        writer = new StreamWriter(fullPath, append)
        {
            AutoFlush = true
        };
    }

    public ILogger CreateLogger(string categoryName)
        => new FileLogger(categoryName, writer, lockObject, minLevel);

    public void Dispose()
    {
        writer.Dispose();
        GC.SuppressFinalize(this);
    }
}

file class FileLogger(string category, StreamWriter writer, Lock lockObject, LogLevel minLevel) : ILogger
{
    private readonly string category = category;
    private readonly StreamWriter writer = writer;
    private readonly Lock lockObject = lockObject;
    private readonly LogLevel minLevel = minLevel;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

    public bool IsEnabled(LogLevel level) => level >= minLevel && level != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message))
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        lock (lockObject)
        {
            writer.Write($"{timestamp} [{logLevel}] {category}: ");
            writer.WriteLine(message);
            if (exception is not null)
                writer.WriteLine(exception);
        }
    }
}
