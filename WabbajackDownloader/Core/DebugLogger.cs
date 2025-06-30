using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace WabbajackDownloader.Core;

internal class DebugLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string name) => new DebugLogger(name);

    public void Dispose() { }
}

file class DebugLogger(string name) : ILogger
{
    private readonly string name = name;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

    public bool IsEnabled(LogLevel logLevel) => Debugger.IsAttached && logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message))
            return;

        message = $"{logLevel}: {message}";
        if (exception != null)
            message += Environment.NewLine + Environment.NewLine + exception;

        Debug.WriteLine(message, name);
    }
}