using Avalonia.Controls;
using Avalonia.Media;

namespace WabbajackDownloader;

public partial class Splashscreen : Window
{
    public Splashscreen()
    {
        InitializeComponent();

        // Change text color to silver if system theme is dark
        if (PlatformSettings != null)
        {
            var color = PlatformSettings.GetColorValues();
            if (color.ThemeVariant == Avalonia.Platform.PlatformThemeVariant.Dark)
                loadingText.Foreground = SolidColorBrush.Parse("#C0C0C0");
        }
    }
}