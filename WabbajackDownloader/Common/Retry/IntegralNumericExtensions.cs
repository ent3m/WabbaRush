using System.Numerics;

namespace WabbajackDownloader.Common.Retry;

public static class IntegralNumericExtensions
{
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
