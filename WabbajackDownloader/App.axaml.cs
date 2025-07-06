using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using WabbajackDownloader.Views;
using Xilium.CefGlue;
using Xilium.CefGlue.Common;
using WabbajackDownloader.Logging;
using WabbajackDownloader.Configuration;

namespace WabbajackDownloader
{
    public partial class App : Application
    {
#pragma warning disable CS8618 // these are created in Initialize()
        private AppSettings settings;
        private ILoggerProvider loggerProvider;
#pragma warning restore CS8618

        public override void Initialize()
        {
            // load app settings
            var settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
            settings = AppSettings.LoadOrGetDefaultSettings(settingsPath);

            // manage cef logging
            CefRuntimeLoader.Initialize(new CefSettings()
            {
                LogFile = Path.Combine(Directory.GetCurrentDirectory(), "cef-debug.log"),
                LogSeverity = settings.CefLogLevel
            });

            // configure logging
#if DEBUG
            loggerProvider = new DebugLoggerProvider();
#else
            var logPath = Path.Combine(AppContext.BaseDirectory, "debug.log");
            loggerProvider = new FileLoggerProvider(logPath, settings.LogLevel);
#endif
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = new MainWindow(settings, loggerProvider);
                desktop.MainWindow = window;
                // save user settings to file when app shuts down
                desktop.ShutdownRequested += (_, _) => settings.SaveSettings();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}