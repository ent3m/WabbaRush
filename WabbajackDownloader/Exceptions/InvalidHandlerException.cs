using System;

namespace WabbajackDownloader.Exceptions;

public class InvalidHandlerException(string message) : Exception(message);
