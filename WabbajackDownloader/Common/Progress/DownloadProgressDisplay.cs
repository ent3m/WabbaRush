namespace WabbajackDownloader.Common.Progress;

internal sealed class DownloadProgressDisplay : ProgressDisplay<long>
{
    private readonly TimeProvider _timeProvider;
    private long _lastValue;
    private long _lastTimestamp;
    private double _smoothedRate;
    private bool _isFirstRateCalculation = true;

    // Adjust this to change responsiveness.
    // Lower = smoother but slower to adapt. Higher = faster to adapt but twitchier.
    private const double SmoothingFactor = 0.2;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(500);

    public string FormattedFileSize { get; }

    public DownloadProgressDisplay(long maximum, string title) : base(0, maximum, title)
    {
        _timeProvider = TimeProvider.System;
        _lastTimestamp = _timeProvider.GetTimestamp();
        _lastValue = 0;
        FormattedFileSize = $"({maximum.FormatByteSize()})";
    }

    public override void Report(long value)
    {
        base.Report(value);

        // Report download speed using Time-Throttled Calculation and Exponential Moving Average
        var now = _timeProvider.GetTimestamp();
        var elapsedTime = _timeProvider.GetElapsedTime(_lastTimestamp, now);

        if (elapsedTime < UpdateInterval)
            return;

        double elapsedSeconds = elapsedTime.TotalSeconds;
        double rawBytesPerSecond = (value - _lastValue) / elapsedSeconds;

        if (_isFirstRateCalculation)
        {
            _smoothedRate = rawBytesPerSecond;
            _isFirstRateCalculation = false;
        }
        else
            _smoothedRate = (SmoothingFactor * rawBytesPerSecond) + ((1 - SmoothingFactor) * _smoothedRate);

        Description = _smoothedRate.FormatByteRate();

        _lastValue = value;
        _lastTimestamp = now;
    }

    /// <summary>
    /// Reset progress to 0 and clear accumulated timing data.
    /// </summary>
    public void ResetProgress()
    {
        _lastTimestamp = _timeProvider.GetTimestamp();
        _lastValue = 0;
        _smoothedRate = 0;
        _isFirstRateCalculation = true;
        base.Report(_lastValue);
    }
}
