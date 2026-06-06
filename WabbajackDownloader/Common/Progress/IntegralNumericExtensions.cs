namespace WabbajackDownloader.Common.Progress;

internal static class IntegralNumericExtensions
{
    public static string FormatByteSize(this long bytes)
        => FormatMetric(bytes, SizeUnits);

    public static string FormatByteRate(this double bytesPerSecond)
    {
        if (double.IsNaN(bytesPerSecond) || bytesPerSecond < 0) return "0 B/s";
        if (double.IsInfinity(bytesPerSecond)) return "∞ B/s";

        return FormatMetric(bytesPerSecond, RateUnits);
    }

    private static string FormatMetric(double value, ReadOnlySpan<string> units)
    {
        // Avoid 999.995 being rounded up to 1000 under under "0.##" format.
        const double threshold = 999.995;
        int unitIndex = 0;
        while (value >= threshold && unitIndex < units.Length - 1)
        {
            value /= 1000;
            unitIndex++;
        }
        return $"{value:0.##} {units[unitIndex]}";
    }

    private static readonly string[] SizeUnits = ["B", "KB", "MB", "GB", "TB", "PB"];
    private static readonly string[] RateUnits = ["B/s", "KB/s", "MB/s", "GB/s", "TB/s"];
}
