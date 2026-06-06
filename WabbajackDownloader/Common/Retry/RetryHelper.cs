using System.Numerics;

namespace WabbajackDownloader.Common.Retry;

internal static class RetryHelper
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

    public static int GetNextDelay(this RetryOptions options, int currentDelay, out int actualDelay)
    {
        actualDelay = currentDelay + Random.Shared.Next(options.Jitter);
        return (int)Math.Min(currentDelay * options.Multiplier, int.MaxValue);
    }
}
