﻿using WabbajackDownloader.Hashing;

namespace WabbajackDownloader.ModList;

internal class PartDefinition
{
    public long Size { get; set; }
    public long Offset { get; set; }
    public Hash Hash { get; set; }
    public long Index { get; set; }
}
