namespace WabbajackDownloader.Common.Retry;

public record RetryOptions(int Retries, int Delay, int Multiplier, int Jitter);
