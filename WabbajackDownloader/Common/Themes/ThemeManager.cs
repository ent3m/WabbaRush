using Avalonia;
using Avalonia.Styling;
using System.ComponentModel;

namespace WabbajackDownloader.Common.Themes;

internal partial class ThemeManager : INotifyPropertyChanged
{
    // This will be resolved in App.axaml.cs
    public static ThemeManager Instance
    {
        get;
        set
        {
            if (value != null && field == null)
                field = value;
        }
    } = null!;

    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly SettingsManager _settingsManager;

    public ThemeManager(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        IsDarkMode = _settingsManager.Settings.IsDarkMode;
    }

    public bool? IsDarkMode
    {
        get;
        set
        {
            if (field == value) return;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDarkMode)));

            _settingsManager.Settings.IsDarkMode = value;

            if (Application.Current is null) return;
            Application.Current.RequestedThemeVariant = value switch
            {
                true => ThemeVariant.Dark,
                false => ThemeVariant.Light,
                _ => ThemeVariant.Default
            };
        }
    }
}
