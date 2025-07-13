using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WabbajackDownloader.Extensions;

namespace WabbajackDownloader.Common;

// Implement the circuit breaker pattern by providing auto retry with exponential backoff and random jitter
internal class CircuitBreaker(int retries, int delay, int multiplier, int jitter)
{
    public int Retries { get; } = retries;
    public int Delay { get; } = delay;
    public int Multiplier { get; } = multiplier;
    public int Jitter { get; } = jitter;

    private readonly Random random = new();

    public async Task AutoRetryAsync(Func<Task> function,
        Predicate<Exception>? unignoreExceptions,
        ILogger? logger,
        string? taskName,
        CancellationToken cancellationToken)
    {
        int retryCount = 0;
        int delay = Delay;
        string name = string.IsNullOrEmpty(taskName) ? "Task" : taskName;

    BEGIN:
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await function();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (unignoreExceptions != null && unignoreExceptions(ex))
                throw;

            if (retryCount < Retries)
            {
                retryCount++;
                var actualDelay = delay + random.Next(Jitter);
                logger?.LogWarning(ex.GetBaseException(), "{name} failed. Attempting {retryCount} retry in {delay} milliseconds.", name, retryCount.DisplayWithSuffix(), actualDelay);
                await Task.Delay(actualDelay, cancellationToken);
                delay *= Multiplier;
                goto BEGIN;
            }
            else
            {
                logger?.LogError(ex, "{name} failed. No retries remaining.", name);
                throw;
            }
        }
    }

    public async Task<T> AutoRetryAsync<T>(Func<Task<T>> function,
        Predicate<Exception>? unignoreExceptions,
        ILogger? logger,
        string? taskName,
        CancellationToken cancellationToken)
    {
        int retryCount = 0;
        int delay = Delay;
        string name = string.IsNullOrEmpty(taskName) ? "Task" : taskName;

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
                throw;

            if (retryCount < Retries)
            {
                retryCount++;
                var actualDelay = delay + random.Next(Jitter);
                logger?.LogWarning(ex.GetBaseException(), "{name} failed. Attempting {retryCount} retry in {delay} milliseconds.", name, retryCount.DisplayWithSuffix(), actualDelay);
                await Task.Delay(actualDelay, cancellationToken);
                delay *= Multiplier;
                goto BEGIN;
            }
            else
            {
                logger?.LogError(ex, "{name} failed. No retries remaining.", name);
                throw;
            }
        }
    }
}
