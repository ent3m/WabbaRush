using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WabbajackDownloader.Extensions;

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

    public static string DisplayWithSuffix<T>(this T num) where T : IBinaryInteger<T>
    {
        string number = num.ToString(null, null);
        if (number.EndsWith("11")) return number + "th";
        if (number.EndsWith("12")) return number + "th";
        if (number.EndsWith("13")) return number + "th";
        if (number.EndsWith('1')) return number + "st";
        if (number.EndsWith('2')) return number + "nd";
        if (number.EndsWith('3')) return number + "rd";
        return number + "th";
    }
}
