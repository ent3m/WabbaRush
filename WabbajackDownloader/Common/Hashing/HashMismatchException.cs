namespace WabbajackDownloader.Common.Hashing;

/// <summary>
/// Thrown when computed hash value does not match expected value
/// </summary>
internal sealed class HashMismatchException(string? message, Exception? innerException = null) : Exception(message, innerException);
