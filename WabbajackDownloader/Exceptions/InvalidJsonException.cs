using System;

namespace WabbajackDownloader.Exceptions;

/// <summary>
/// Thrown when http json response cannot be interpreted
/// </summary>
public class InvalidJsonResponseException(string? message, Exception? innerException = null) : Exception(message, innerException);