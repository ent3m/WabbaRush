using Avalonia.Controls;
using System;
using System.Collections.Concurrent;
using System.Threading;
using WabbajackDownloader.Common;
using WabbajackDownloader.Extensions;

namespace WabbajackDownloader.Core;

/// <summary>
/// A pool of progress display for nexus download
/// </summary>
internal class DownloadProgressPool(Panel panel) : IDisposable
{
    private readonly Controls container = panel.Children;
    private readonly ConcurrentBag<ProgressDisplay<long>> bag = [];
    private readonly SynchronizationContext synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
    private ProgressDisplay<long>? fetchedProgress;
    private readonly Lock lockObject = new();

    /// <summary>
    /// Rent a progress display for a specific download
    /// </summary>
    public ProgressDisplay<long> Get(NexusDownload download)
    {
        lock (lockObject)
        {
            synchronizationContext.Send(FetchOrCreateProgress, download);
            return fetchedProgress!;
        }
    }

    /// <summary>
    /// Return the progress display to the pool
    /// </summary>
    public void Return(ProgressDisplay<long> progress)
    {
        synchronizationContext.Post(ReturnProgress, progress);
    }

    /// <summary>
    /// Get progres display from bag or create a new one and add it to container
    /// </summary>
    private void FetchOrCreateProgress(object? item)
    {
        if (item is NexusDownload download)
        {
            if (!bag.TryTake(out ProgressDisplay<long>? progress))
                progress = new ProgressDisplay<long>();

            progress.ProgressBar.Value = 0;
            progress.ProgressBar.Maximum = download.FileSize;
            progress.ProgressBar.ProgressTextFormat = $"({download.FileSize.DisplayByteSize()}) {download.FileName}";
            container.Add(progress.ProgressBar);

            fetchedProgress = progress;
        }
        else
            throw new InvalidCastException($"Cannot get {nameof(ProgressDisplay<long>)} for {nameof(item)}. Item is not of type {typeof(NexusDownload)}.");
    }

    /// <summary>
    /// Remove progress bar from container and return progress display to bag
    /// </summary>
    private void ReturnProgress(object? item)
    {
        if (item is ProgressDisplay<long> progress)
        {
            container.Remove(progress.ProgressBar);
            bag.Add(progress);
        }
        else
            throw new InvalidCastException($"Cannot return {nameof(item)} to bag. Item is not of type {typeof(ProgressDisplay<long>)}.");
    }

    public void Dispose()
    {
        bag.Clear();
    }
}
