namespace WabbajackDownloader.Common.Retry;

internal sealed record RetryOptions(int MaxRetries, int BaseDelay, double Multiplier, int Jitter);
