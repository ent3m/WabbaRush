namespace WabbajackDownloader.Common.Retry;

/// <summary>
/// Implement the retry pattern by providing auto retry with exponential backoff.
/// </summary>
internal sealed class RetryHandler<TCaller>(RetryOptions options, ILogger<TCaller> logger)
{
    public Task AutoRetryAsync(
        Func<Task> function,
        Predicate<Exception>? isFatalError,
        CancellationToken cancellationToken)
        => AutoRetryAsync<bool>(
            async () => { await function(); return false; },
            isFatalError,
            cancellationToken);

    public async Task<T> AutoRetryAsync<T>(
        Func<Task<T>> function,
        Predicate<Exception>? isFatalError,
        CancellationToken cancellationToken)
    {
        int retryCount = 0;
        int delay = options.BaseDelay;

    BEGIN:
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await function();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Operation was cancelled. Retry aborted.");
            throw;
        }
        catch (Exception ex)
        {
            if (isFatalError != null && isFatalError(ex))
            {
                throw;
            }

            if (retryCount < options.MaxRetries)
            {
                retryCount++;
                delay = options.GetNextDelay(delay, out var actualDelay);

                logger.LogWarning(ex, "Operation failed. Attempting {retryCount} retry in {delay} milliseconds.", retryCount.DisplayWithSuffix(), actualDelay);
                await Task.Delay(actualDelay, cancellationToken);

                goto BEGIN;
            }
            else
            {
                logger.LogError(ex, "Operation failed. No retries remaining.");
                throw;
            }
        }
    }
}
