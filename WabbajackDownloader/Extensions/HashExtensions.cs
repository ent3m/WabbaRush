using Microsoft.Extensions.Logging;
using WabbajackDownloader.Exceptions;
using WabbajackDownloader.Hashing;

namespace WabbajackDownloader.Extensions;

internal static class HashExtensions
{
    public static void ThrowOnMismatch(this Hash computed, Hash expected, string name, ILogger? logger)
    {
        if (computed.Equals(expected)) return;

        logger?.LogError("Hash does not match pre-computed value for {name}.", name);
        throw new HashMismatchException($"Computed hash does not match expected value.\nExpected: {expected}\nComputed: {computed}", null);
    }
}
