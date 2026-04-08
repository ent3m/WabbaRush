namespace WabbajackDownloader.Common.Hashing;

/// <summary>
/// Thrown when computed hash value does not match expected value
/// </summary>
public class HashMismatchException(string? message, Exception? innerException = null) : Exception(message, innerException);
