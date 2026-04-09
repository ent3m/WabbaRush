using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using System.IO;
using WabbajackDownloader.Common.Configuration;
using WabbajackDownloader.Common.Platform;
using WabbajackDownloader.Features.WabbajackRepo;
using Waypoint;

namespace WabbajackDownloader.Features.Dashboard;

public partial class MainWindowViewModel : ObservableValidator, IDisposable
{
    private readonly SettingsManager _settingsManager;
    private readonly AppSettings _settings;
    private readonly INavigator _navigator;
    private readonly IStorageService _storageService;
    private readonly ILauncherService _launcherService;
    private readonly CancellationTokenSource _cts = new();

    #region Observables
    [ObservableProperty]
    public partial ModListMetadata[] ModLists { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadModsCommand))]
    public partial int SelectedModListIndex { get; set; }
    partial void OnSelectedModListIndexChanged(int value)
    {
        if (SelectedModListIndex >= 0 && SelectedModListIndex < ModLists.Length)
            _settings.SelectedModList = ModLists[value].Title;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadModsCommand))]
    public partial bool UseLocalFile { get; set; }
    partial void OnUseLocalFileChanged(bool value)
    {
        _settings.UseLocalWabbajackFile = value;
    }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(DownloadModsCommand))]
    [CustomValidation(typeof(MainWindowViewModel), nameof(ValidateLocalFilePath))]
    public partial string? LocalFilePath { get; set; }
    partial void OnLocalFilePathChanged(string? value)
    {
        _settings.WabbajackFile = value;
    }
    public static ValidationResult? ValidateLocalFilePath(string? value, ValidationContext _)
    {
        if (value is null) return ValidationResult.Success;
        return File.Exists(value) ? ValidationResult.Success : new ValidationResult("File not found.");
    }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(DownloadModsCommand))]
    [CustomValidation(typeof(MainWindowViewModel), nameof(ValidateDownloadFolderPath))]
    public partial string? DownloadFolderPath { get; set; }
    partial void OnDownloadFolderPathChanged(string? value)
    {
        _settings.DownloadFolder = value;
    }
    public static ValidationResult? ValidateDownloadFolderPath(string? value, ValidationContext _)
    {
        if (value is null) return ValidationResult.Success;
        return Directory.Exists(value) ? ValidationResult.Success : new ValidationResult("Directory not found.");
    }

    [ObservableProperty]
    public partial int MaxDownloadSize { get; set; }
    partial void OnMaxDownloadSizeChanged(int value)
    {
        _settings.MaxDownloadSizeMB = value;
    }

    [ObservableProperty]
    public partial int MaxConcurrency { get; set; }
    partial void OnMaxConcurrencyChanged(int value)
    {
        _settings.MaxConcurrency = value;
    }

    [ObservableProperty]
    public partial string? InfoText { get; set; }
    #endregion

    public MainWindowViewModel(SettingsManager settingsManager, RepositoriesDownloader repositoriesDownloader, INavigator navigator, IStorageService storageService, ILauncherService launcherService)
    {
        _settingsManager = settingsManager;
        _settings = settingsManager.Settings;
        _navigator = navigator;
        _storageService = storageService;
        _launcherService = launcherService;
        ModLists = repositoriesDownloader.Repositories ?? [];
        UseLocalFile = _settings.UseLocalWabbajackFile;
        LocalFilePath = _settings.WabbajackFile;
        DownloadFolderPath = _settings.DownloadFolder;
        MaxDownloadSize = _settings.MaxDownloadSizeMB;
        MaxConcurrency = _settings.MaxConcurrency;
        SelectedModListIndex = Array.FindIndex(ModLists, m => m.Title == _settings.SelectedModList);
    }

    #region Commands
    [RelayCommand]
    private void OpenRepo() => _launcherService.LaunchUriAsync(new Uri(RepoUrl));
    public const string RepoUrl = "https://github.com/ent3m/WabbaRush";

    [RelayCommand]
    private async Task OpenWabbajackFilePickerAsync()
    {
        var files = await _storageService.OpenFilePickerAsync(_wabbajackFilePickerOptions);
        if (files.Count == 0) return;

        var storageFile = files[0];
        LocalFilePath = storageFile.TryGetLocalPath() ?? storageFile.Path.ToString();
    }
    private static readonly FilePickerFileType _wabbajackFileType = new("Wabbajack file")
    {
        Patterns = ["*.wabbajack"]
    };
    private static readonly FilePickerOpenOptions _wabbajackFilePickerOptions = new()
    {
        Title = "Select wabbajack file for mod list extraction",
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
    }
    private static readonly FolderPickerOpenOptions _downloadFolderPickerOptions = new()
    {
        Title = "Select folder to store downloaded mods",
        AllowMultiple = false
    };

    [RelayCommand]
    private async Task OpenNexusWindowAsync()
    {
        await _navigator.ShowWindowAsync<NexusWindow, MainWindow>();
    }

    [RelayCommand(CanExecute = nameof(CanDownloadMods))]
    private async Task DownloadModsAsync(CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);

        _settingsManager.Save();
        InfoText = "Starting mod download...";
    }
    private bool CanDownloadMods()
    {
        if (HasErrors)
            return false;

        if (UseLocalFile)
            return !string.IsNullOrEmpty(DownloadFolderPath) && !string.IsNullOrEmpty(LocalFilePath);
        else
            return !string.IsNullOrEmpty(DownloadFolderPath) && SelectedModListIndex >= 0 && SelectedModListIndex < ModLists.Length;
    }
    #endregion

    #region Cleanup
    public void Dispose()
    {
        _cts.Cancel();
        Cleanup();
        _cts.Dispose();
    }
    private void Cleanup()
    {

    }
    #endregion
}
