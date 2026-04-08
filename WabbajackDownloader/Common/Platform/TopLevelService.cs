using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace WabbajackDownloader.Common.Platform;

internal sealed class TopLevelService : IStorageService, ILauncherService
{
    private static TopLevel GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
            return TopLevel.GetTopLevel(window) ?? throw new InvalidOperationException("TopLevel could not be resolved from the main window.");
        else
            throw new InvalidOperationException("Application lifetime is not IClassicDesktopStyleApplicationLifetime.");
    }

    public Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
        => GetTopLevel().StorageProvider.OpenFilePickerAsync(options);

    public Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
        => GetTopLevel().StorageProvider.OpenFolderPickerAsync(options);

    public Task<bool> LaunchUriAsync(Uri uri)
        => GetTopLevel().Launcher.LaunchUriAsync(uri);
}
