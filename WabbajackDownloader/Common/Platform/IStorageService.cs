using Avalonia.Platform.Storage;

namespace WabbajackDownloader.Common.Platform;

/// <summary>
/// Abstracts Avalonia's TopLevel-bound StorageProvider for use in ViewModels.
/// </summary>
public interface IStorageService
{
    Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options);
    Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options);
}
