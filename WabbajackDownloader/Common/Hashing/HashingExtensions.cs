namespace WabbajackDownloader.Common.Hashing;

public static class HashingExtensions
{
    public static void ThrowOnMismatch(this Hash computed, Hash expected, string name)
    {
        if (computed.Equals(expected)) return;

        throw new HashMismatchException($"Computed hash does not match expected value for {name}.\nExpected: {expected}\nComputed: {computed}", null);
    }
}
