namespace WabbajackDownloader.Common.Progress;

internal static class IntegralNumericExtensions
{
    public static string DisplayByteSize(this long bytes)
    {
        if (bytes < 0)
            return "NaN";

        if (bytes < 1024)
            return $"{bytes} B";

        double size = bytes;
        int order = -1;
        do
        {
            size /= 1024;
            order++;
        }
        while (size > 1024 && order < sizeSuffixes.Length - 1);

        return ($"{size:0.#} {sizeSuffixes[order]}");
    }
    private readonly static string[] sizeSuffixes = ["KB", "MB", "GB", "TB", "PB"];
}
