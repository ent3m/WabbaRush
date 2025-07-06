using System;
using System.Text.Json;

namespace WabbajackDownloader.Exceptions;

/// <summary>
/// Thrown when http json response cannot be interpreted
/// </summary>
internal class InvalidJsonResponseException(string? message, Exception? innerException = null) : Exception(message, innerException);