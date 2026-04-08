namespace WabbajackDownloader.Common.Result;

public readonly record struct Result<T>(T? Value, Error? Error)
{
    [Obsolete("Use Result.Success() or Result.Failure() instead.", error: true)]
    public Result() : this(default, default) => _isInitialized = false;

    // In 'default(Result<T>)', this is false.
    private readonly bool _isInitialized = true;
    public readonly bool IsSuccess => _isInitialized && Error == null;

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(Error error) => new(default, error);
}
