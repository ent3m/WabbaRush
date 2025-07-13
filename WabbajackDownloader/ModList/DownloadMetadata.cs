using WabbajackDownloader.Hashing;

namespace WabbajackDownloader.ModList;

public class DownloadMetadata
{
    public Hash Hash { get; set; }
    public long Size { get; set; }
    public long NumberOfArchives { get; set; }
    public long SizeOfArchives { get; set; }
    public long NumberOfInstalledFiles { get; set; }
    public long SizeOfInstalledFiles { get; set; }
    /// <summary>
    /// The size of wabbajack file combined with downloaded archives
    /// </summary>
    public long TotalSize => Size + SizeOfArchives;
}
