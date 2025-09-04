using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using WabbajackDownloader.Configuration;
using WabbajackDownloader.Core;
using WabbajackDownloader.Logging;
using WabbajackDownloader.ModList;
using WabbajackDownloader.Views;
using Xilium.CefGlue;
using Xilium.CefGlue.Common;

namespace WabbajackDownloader
{
    public partial class App : Application
    {
#pragma warning disable CS8618 // these are created in Initialize()
        private AppSettings settings;
        private ILoggerProvider loggerProvider;
        private Window splashscreen;
#pragma warning restore CS8618

        public override void Initialize()
        {
            // load app settings
            var settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
            settings = AppSettings.LoadOrGetDefaultSettings(settingsPath);

            // manage cef logging
            CefRuntimeLoader.Initialize(new CefSettings()
            {
                LogFile = Path.Combine(AppContext.BaseDirectory, "cef-debug.log"),
                LogSeverity = settings.CefLogLevel,
                Locale = "en-US",
            });

            // configure logging
#if DEBUG
            loggerProvider = new DebugLoggerProvider();
#else
            var logPath = Path.Combine(AppContext.BaseDirectory, "debug.log");
            loggerProvider = new FileLoggerProvider(logPath, settings.LogLevel, settings.AppendDebugLog);
#endif
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                splashscreen = new Splashscreen();
                desktop.MainWindow = splashscreen;
            }

            RepositoriesDownloader.FetchRepositoriesAsync(settings.MaxConcurrency, settings.HttpTimeout,
                loggerProvider.CreateLogger(nameof(RepositoriesDownloader)), CancellationToken.None)
                .ContinueWith(r => Dispatcher.UIThread.Post(() => CompleteApplicationStart(r.Result)));

            base.OnFrameworkInitializationCompleted();
        }

        private void CompleteApplicationStart(ModListMetadata[]? repositories)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = new MainWindow(repositories, settings, loggerProvider);
                desktop.MainWindow = window;
                window.Show();
                splashscreen.Close();
            }
        }
    }
}