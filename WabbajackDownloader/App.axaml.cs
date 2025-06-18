using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using WabbajackDownloader.Views;

namespace WabbajackDownloader
{
    public partial class App : Application
    {
        public override void Initialize()
        {
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

        public static Window MainWindow { get; private set; }
        public static Window? SigninWindow { get; set; }
    }
}