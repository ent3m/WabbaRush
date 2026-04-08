namespace WabbajackDownloader.Common.Retry;

// Implement the retry pattern by providing auto retry with exponential backoff and random jitter
public class RetryHandler<TCaller>(RetryOptions options, ILogger<TCaller> logger)
{
    private readonly RetryOptions _options = options;
    private readonly ILogger<TCaller> _logger = logger;
    private readonly Random _random = Random.Shared;

    public Task AutoRetryAsync(
        Func<Task> function,
        Predicate<Exception>? unignoreExceptions,
        CancellationToken cancellationToken)
        => AutoRetryAsync<bool>(
            async () => { await function(); return false; },
            unignoreExceptions,
            cancellationToken);

    public async Task<T> AutoRetryAsync<T>(
        Func<Task<T>> function,
        Predicate<Exception>? unignoreExceptions,
        CancellationToken cancellationToken)
    {
        int retryCount = 0;
        int delay = _options.Delay;

    BEGIN:
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await function();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (unignoreExceptions != null && unignoreExceptions(ex))
            {
                throw;
            }

            if (retryCount < _options.Retries)
            {
                retryCount++;
                var actualDelay = delay + _random.Next(_options.Jitter);
                _logger.LogWarning(ex.GetBaseException(), "Operation failed. Attempting {retryCount} retry in {delay} milliseconds.", retryCount.DisplayWithSuffix(), actualDelay);
                await Task.Delay(actualDelay, cancellationToken);
                delay *= _options.Multiplier;
                goto BEGIN;
            }
            else
            {
                _logger.LogError(ex, "Operation failed. No retries remaining.");
                throw;
            }
        }
    }
}
