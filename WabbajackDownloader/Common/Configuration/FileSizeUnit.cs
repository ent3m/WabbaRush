namespace WabbajackDownloader.Common.Configuration;

internal enum FileSizeUnit
{
    KB,
    MB,
    GB,
}

internal static class FileSizeUnitExtensions
{
    public static long ToBytes(this FileSizeUnit unit, int size)
    {
        return unit switch
        {
            FileSizeUnit.KB => size * 1000L,
            FileSizeUnit.MB => size * 1000L * 1000L,
            FileSizeUnit.GB => size * 1000L * 1000L * 1000L,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }
}