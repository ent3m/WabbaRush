using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using WabbajackDownloader.Core;
using WabbajackDownloader.Views;
using Xilium.CefGlue;
using Xilium.CefGlue.Common;

namespace WabbajackDownloader
{
    public partial class App : Application
    {
#pragma warning disable CS8618 
        public static AppSettings Settings { get; private set; } // created in Initialize()
        public static ILogger Logger { get; private set; } // created in Initialize()
        public static Window MainWindow { get; private set; } // created in OnFrameworkInitializationCompleted()
#pragma warning restore CS8618
        public static Window? SigninWindow { get; set; }

        public override void Initialize()
        {
            // load app settings
            var settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
            Settings = AppSettings.LoadSettings(settingsPath);

            // manage cef settings
            CefRuntimeLoader.Initialize(new CefSettings()
            {
                LogFile = Path.Combine(Directory.GetCurrentDirectory(), "cef-debug.log"),
                LogSeverity = Settings.CefLogLevel
            });

            // configure logging
#if DEBUG
            var factory = new DebugLoggerProvider();
            Logger = factory.CreateLogger("WabbajackDownloader");
#else
            var logPath = Path.Combine(AppContext.BaseDirectory, "debug.log");
            var factory = new FileLoggerProvider(logPath, Settings.LogLevel);
            Logger = factory.CreateLogger("WabbajackDownloader");
#endif

            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = new MainWindow();
                desktop.MainWindow = window;
                MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}