using WabbajackDownloader.Common.Hashing;

namespace WabbajackDownloader.Features.WabbajackModList;

public class PartDefinition
{
    public long Size { get; set; }
    public long Offset { get; set; }
    public Hash Hash { get; set; }
    public long Index { get; set; }
}
