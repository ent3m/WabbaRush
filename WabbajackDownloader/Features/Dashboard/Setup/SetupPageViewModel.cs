using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WabbajackDownloader.Common.Platform;
using WabbajackDownloader.Features.Frame;
using WabbajackDownloader.Features.WabbajackRepo;
using Waypoint;

namespace WabbajackDownloader.Features.Dashboard;

internal sealed partial class SetupPageViewModel : ObservableObject
{
    private AppSettings Settings => _settingsManager.Settings;
    private readonly SettingsManager _settingsManager;
    private readonly INavigator _navigator;
    private readonly IStorageService _storageService;
    private readonly ILogger _logger;

    #region Observables
    [ObservableProperty]
    public partial ModListMetadata[] ModLists { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ProceedCommand))]
    public partial int SelectedModListIndex { get; set; }
    partial void OnSelectedModListIndexChanged(int value)
    {
        if (SelectedModListIndex >= 0 && SelectedModListIndex < ModLists.Length)
            Settings.SelectedModList = ModLists[value].Title;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ProceedCommand))]
    public partial bool UseLocalFile { get; set; }
    partial void OnUseLocalFileChanged(bool value)
    {
        Settings.UseLocalWabbajackFile = value;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ProceedCommand))]
    public partial string? LocalFilePath { get; set; }
    partial void OnLocalFilePathChanged(string? value)
    {
        Settings.WabbajackFile = value;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ProceedCommand))]
    public partial string? DownloadFolderPath { get; set; }
    partial void OnDownloadFolderPathChanged(string? value)
    {
        Settings.DownloadFolder = value;
    }
    #endregion

    public SetupPageViewModel(SettingsManager settingsManager, RepositoriesDownloader repositoriesDownloader,
        INavigator navigator, IStorageService storageService, ILogger<SetupPageViewModel> logger)
    {
        _settingsManager = settingsManager;
        _navigator = navigator;
        _storageService = storageService;
        _logger = logger;
        ModLists = repositoriesDownloader.Repositories ?? [new() { Title = "Something" }, new() { Title = "Went" }, new() { Title = "Wrong" }];
        UseLocalFile = Settings.UseLocalWabbajackFile;
        LocalFilePath = Settings.WabbajackFile;
        DownloadFolderPath = Settings.DownloadFolder;
        SelectedModListIndex = Array.FindIndex(ModLists, m => m.Title == Settings.SelectedModList);
    }

    #region Commands
    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        _settingsManager.Save();
        await _navigator.NavigateAsync<SettingsPage, Shell>();
    }

    [RelayCommand]
    private async Task OpenWabbajackFilePickerAsync()
    {
        var files = await _storageService.OpenFilePickerAsync(_wabbajackFilePickerOptions);
        if (files.Count == 0) return;

        var storageFile = files[0];
        LocalFilePath = storageFile.TryGetLocalPath() ?? storageFile.Path.ToString();

        _logger.LogDebug("Wabbajack file set to {FilePath}.", LocalFilePath);
    }
    private static readonly FilePickerFileType _wabbajackFileType = new("Wabbajack file")
    {
        Patterns = ["*.wabbajack"]
    };
    private static readonly FilePickerOpenOptions _wabbajackFilePickerOptions = new()
    {
        Title = "Select wabbajack file for modlist extraction",
        AllowMultiple = false,
        FileTypeFilter = [_wabbajackFileType]
    };

    [RelayCommand]
    private async Task OpenDownloadFolderPickerAsync()
    {
        var folder = await _storageService.OpenFolderPickerAsync(_downloadFolderPickerOptions);
        if (folder.Count == 0) return;

        var downloadFolder = folder[0];
        DownloadFolderPath = downloadFolder.TryGetLocalPath() ?? downloadFolder.Path.ToString();

        _logger.LogDebug("Download folder set to {FolderPath}.", LocalFilePath);
    }
    private static readonly FolderPickerOpenOptions _downloadFolderPickerOptions = new()
    {
        Title = "Select folder to store downloaded mods",
        AllowMultiple = false
    };

    [RelayCommand(CanExecute = nameof(CanProceed))]
    private async Task ProceedAsync()
    {
        _settingsManager.Save();

        // CanProceed guarantees null safety of LocalFilePath and index safety of SelectedModListIndex
        ModListPreparationOptions options;
        if (UseLocalFile)
            options = new LocalModListPreparationOptions(LocalFilePath!);
        else
            options = new RemoteModListPreparationOptions(ModLists[SelectedModListIndex]);

        await _navigator.NavigateAsync<DownloadPage, Shell>(parameter: options);
    }
    private bool CanProceed()
    {
        if (UseLocalFile)
            return !string.IsNullOrEmpty(DownloadFolderPath) && !string.IsNullOrEmpty(LocalFilePath);
        else
            return !string.IsNullOrEmpty(DownloadFolderPath) && SelectedModListIndex >= 0 && SelectedModListIndex < ModLists.Length;
    }
    #endregion
}
