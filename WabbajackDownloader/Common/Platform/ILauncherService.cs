namespace WabbajackDownloader.Common.Platform;

/// <summary>
/// Abstracts Avalonia's TopLevel-bound Launcher for use in ViewModels.
/// </summary>
public interface ILauncherService
{
    Task<bool> LaunchUriAsync(Uri uri);
}
