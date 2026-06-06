using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WabbajackDownloader.Common.Dialogs;
using WabbajackDownloader.Common.Platform;
using WabbajackDownloader.Common.Update;
using WabbajackDownloader.Features.Frame;
using Waypoint;

namespace WabbajackDownloader.Features.Dashboard;

internal sealed partial class SettingsPageViewModel : ObservableObject, INavigable
{
    private readonly ILauncherService _launcherService;
    private readonly INavigator _navigator;
    private readonly SettingsManager _settingsManager;
    private AppSettings Settings => _settingsManager.Settings;
    private readonly UpdateHandler _updateHandler;

    #region Observables
    public int TotalParts
    {
        get;
        set
        {
            int clamped = Math.Clamp(value, SettingsManager.TotalPartsLowerBound, SettingsManager.TotalPartsUpperBound);
            if (SetProperty(ref field, clamped))
            {
                Settings.TotalParts = clamped;
                // Coerce current part to new total
                CurrentPart = Math.Clamp(CurrentPart, SettingsManager.TotalPartsLowerBound, clamped);
            }
        }
    }

    public int CurrentPart
    {
        get;
        set
        {
            int clamped = Math.Clamp(value, SettingsManager.TotalPartsLowerBound, TotalParts);
            if (SetProperty(ref field, clamped))
            {
                Settings.CurrentPart = clamped;
            }
        }
    }

    [ObservableProperty]
    public partial int MaxDownloadSize { get; set; }
    partial void OnMaxDownloadSizeChanged(int value)
    {
        Settings.MaxDownloadSize = value;
    }

    public FileSizeUnit[] FileSizeUnitOptions { get; } = Enum.GetValues<FileSizeUnit>();

    [ObservableProperty]
    public partial FileSizeUnit FileSizeUnit { get; set; }
    partial void OnFileSizeUnitChanged(FileSizeUnit value)
    {
        Settings.FileSizeUnit = value;
    }

    public int Concurrency
    {
        get;
        set
        {
            int clamped = Math.Clamp(value, SettingsManager.ConcurrencyLowerBound, SettingsManager.ConcurrencyUpperBound);
            if (SetProperty(ref field, clamped))
            {
                Settings.MaxConcurrency = clamped;
            }
        }
    }

    public int MaxRetries
    {
        get;
        set
        {
            int clamped = Math.Clamp(value, SettingsManager.RetriesLowerBound, SettingsManager.RetriesUpperBound);
            if (SetProperty(ref field, clamped))
            {
                Settings.RetryOptions = Settings.RetryOptions with { MaxRetries = clamped };
            }
        }
    }

    [ObservableProperty]
    public partial bool ClearDownloadFolder { get; set; }
    partial void OnClearDownloadFolderChanged(bool value)
    {
        Settings.ClearDownloadFolder = value;
    }
    #endregion

    public SettingsPageViewModel(ILauncherService launcherService, INavigator navigator, SettingsManager settingsManager, UpdateHandler updateHandler)
    {
        _launcherService = launcherService;
        _navigator = navigator;
        _settingsManager = settingsManager;
        _updateHandler = updateHandler;
        MaxDownloadSize = Settings.MaxDownloadSize;
        FileSizeUnit = Settings.FileSizeUnit;
        Concurrency = Settings.MaxConcurrency;
        MaxRetries = Settings.RetryOptions.MaxRetries;
        ClearDownloadFolder = Settings.ClearDownloadFolder;
        // Save Settings.CurrentPart to local variable before it gets coerced to 1 by TotalParts setter.
        var currentPart = Settings.CurrentPart;
        TotalParts = Settings.TotalParts;
        // Then restore it after TotalParts is set.
        CurrentPart = currentPart;
    }

    async Task<bool> INavigable.OnNavigatingFromAsync(CancellationToken cancellationToken)
    {
        if (ClearDownloadFolder)
        {
            string downloadFolder = _settingsManager.Settings.DownloadFolder ?? "download folder";
            // Alternatively, call `StreamGeometry.Parse` with a hardcoded string, but currently we are keeping all icons in one resource dictionary
            var icon = Application.Current?.TryGetResource("TrashIcon", null, out object? trashIcon) == true ? trashIcon as Geometry : null;
            ConfirmationOptions options = new(
                Title: "Clear Download Folder",
                Message: $"Are you sure you want to permanently delete files in {downloadFolder}?\nThis will happen immediately before downloads begin.",
                Icon: icon);
            return await _navigator.ShowDialogAsync<ConfirmationDialog, bool>(parameter: options, cancellationToken: cancellationToken);
        }
        return true;
    }

    #region Commands
    [RelayCommand]
    private void OpenRepo() => _launcherService.LaunchUriAsync(new Uri(RepoUrl));
    public const string RepoUrl = "https://github.com/ent3m/WabbaRush/";

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        await _updateHandler.CheckForUpdateAndShowNotificationAsync(default);
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        _settingsManager.Save();
        await _navigator.NavigateAsync<SetupPage, Shell>();
    }
    #endregion
}
