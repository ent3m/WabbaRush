using System;

namespace WabbajackDownloader.Exceptions;

public class NexusDownloadException(string? message, Exception? innerException) : Exception(message, innerException);
