using WabbajackDownloader.Common.Hashing;

namespace WabbajackDownloader.Features.WabbajackRepo;

public class DownloadMetadata
{
    public Hash Hash { get; set; }
    public long Size { get; set; }
    public long NumberOfArchives { get; set; }
    public long SizeOfArchives { get; set; }
    public long NumberOfInstalledFiles { get; set; }
    public long SizeOfInstalledFiles { get; set; }
}
