using WabbajackDownloader.Common.Hashing;

namespace WabbajackDownloader.Features.WabbajackModList;

public class FileDefinition
{
    public string? Author { get; set; }
    public string? OriginalFileName { get; set; }
    public long Size { get; set; }
    public Hash Hash { get; set; }
    public PartDefinition[] Parts { get; set; } = [];
    public string? ServerAssignedUniqueId { get; set; }
    public string MungedName => $"{OriginalFileName}_{ServerAssignedUniqueId}";
}
