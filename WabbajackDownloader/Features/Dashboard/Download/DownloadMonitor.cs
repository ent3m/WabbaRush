using System.Collections.Concurrent;

namespace WabbajackDownloader.Features.Dashboard;

partial class DownloadPageViewModel
{
    /// <summary>
    /// Provides a way to monitor <see cref="DownloadContext"/> for stalled download operations.
    /// </summary>
    /// <remarks>
    /// Upon creation, this monitor runs continuously on the thread pool, periodically evaluating registered download contexts.
    /// Upon disposal, it terminates the monitoring task and clears all registered contexts.
    /// Prefer using <see cref="IAsyncDisposable"/> pattern over <see cref="IDisposable"/>.
    /// <para> When the monitor detects that a download operation has been idle for longer than the specified timeout,
    /// it pauses the download. This triggers an internal state transition that bubbles up to the awaiter as a state report,
    /// allowing the awaiter to either resume the operation or let it be cleaned up. </para>
    /// The monitor automatically detects downloads that are no longer in progress and removes them from its tracking list.
    /// It safely handles concurrent scenarios where a download operation transitions states or
    /// re-registers itself while an evaluation cycle is actively running.
    /// </remarks>
    private sealed class DownloadMonitor : IDisposable, IAsyncDisposable
    {
        private readonly TimeProvider _timeProvider;
        private readonly PeriodicTimer _timer;
        private readonly TimeSpan _timeout;

        private readonly ConcurrentBag<DownloadContext> _incoming;
        private readonly HashSet<DownloadContext> _active;

        private readonly Task _monitorTask;

        public static DownloadMonitor None { get; } = new();

        public DownloadMonitor(int timeout)
        {
            _timeProvider = TimeProvider.System;
            _timer = new(TimeSpan.FromSeconds(1), TimeProvider.System);
            _timeout = TimeSpan.FromSeconds(timeout);

            _incoming = [];
            _active = [];

            _monitorTask = Task.Run(Start).ContinueWith(_ => { _incoming.Clear(); _active.Clear(); });
        }

#pragma warning disable CS8618 // This is intended to be a dummy instance that does nothing, so we can ignore the uninitialized fields.
        private DownloadMonitor()
        {
            _monitorTask = Task.CompletedTask;
        }
#pragma warning restore CS8618 // This is intended to be a dummy instance that does nothing, so we can ignore the uninitialized fields.

        public void Register(DownloadContext downloadContext)
        {
            ObjectDisposedException.ThrowIf(_monitorTask.IsCompleted, typeof(DownloadMonitor));

            _incoming.Add(downloadContext);
        }

        private async Task Start()
        {
            while (await _timer.WaitForNextTickAsync())
            {
                while (_incoming.TryTake(out DownloadContext? context))
                {
                    if (context != null)
                        _active.Add(context);
                }

                var now = _timeProvider.GetTimestamp();

                _active.RemoveWhere(context =>
                {
                    // Remove inactive downloads
                    if (context.State != DownloadState.Downloading)
                        return true;

                    var elapsed = _timeProvider.GetElapsedTime(context.ByteReceivedTimeStamp, now);

                    // Pause and remove stalled downloads
                    if (elapsed > _timeout)
                    {
                        context.Pause();
                        return true;
                    }

                    return false;
                });
            }
        }

        public void Dispose()
        {
            if (_monitorTask.IsCompleted) return;

            _timer.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_monitorTask.IsCompleted) return;

            _timer.Dispose();
            try { await _monitorTask; } catch { }
        }
    }
}
