using System;

namespace WabbajackDownloader.Exceptions;

/// <summary>
/// Invalid http response that cannot be interpreted
/// </summary>
internal class InvalidHttpReponseException(string message) : Exception(message) { }