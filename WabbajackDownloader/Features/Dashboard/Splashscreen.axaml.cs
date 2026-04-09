using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace WabbajackDownloader.Features.Dashboard;

public partial class Splashscreen : Window
{
    public Splashscreen()
    {
        InitializeComponent();

        // Change text color to silver if system theme is dark.
        // We set color here because the app does not use theme resources.
        // Only this text matters because it is displayed against system's background.
        if (this.GetPlatformSettings() is IPlatformSettings settings)
        {
            var color = settings.GetColorValues();
            if (color.ThemeVariant == PlatformThemeVariant.Dark)
                loadingText.Foreground = SolidColorBrush.Parse("#C0C0C0");
        }
    }
}