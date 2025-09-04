using System;

namespace WabbajackDownloader.Exceptions
{
    internal class InvalidHandlerException(string message) : Exception(message)
    {
    }
}
