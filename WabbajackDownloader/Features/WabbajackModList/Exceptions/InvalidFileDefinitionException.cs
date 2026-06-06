namespace WabbajackDownloader.Features.WabbajackModList;

/// <summary>
/// Thrown when http json response cannot be interpreted
/// </summary>
internal sealed class InvalidFileDefinitionException(string? message, Exception? innerException = null) : Exception(message, innerException);