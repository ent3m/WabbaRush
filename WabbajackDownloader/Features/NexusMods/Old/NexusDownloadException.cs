namespace WabbajackDownloader.Features.NexusMods;

public class NexusDownloadException(string? message, Exception? innerException) : Exception(message, innerException);
