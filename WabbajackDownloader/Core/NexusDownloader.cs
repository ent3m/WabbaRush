using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WabbajackDownloader.Cef;
using WabbajackDownloader.Common;
using WabbajackDownloader.Configuration;
using WabbajackDownloader.Extensions;
using WabbajackDownloader.Hashing;

namespace WabbajackDownloader.Core;

internal class NexusDownloader : IDisposable
{
    private static readonly string[] acceptedExtensions = [".zip", ".rar", ".7z"];

    private readonly string downloadFolder;
    private readonly IReadOnlyList<NexusDownload> downloads;
    private readonly long maxDownloadSize;
    private readonly int bufferSize;
    private readonly bool checkHash;
    private readonly bool discoverExistingFiles;
    private readonly TimeSpan timeout;
    private readonly ILogger? logger;
    private readonly DownloadProgressPool? progressPool;
    private readonly CircuitBreaker circuitBreaker;

    private Dictionary<string, long>? existingFiles;
    private readonly HttpClient client;
    private readonly SemaphoreSlim semaphore;

    private readonly AutoDownloadCefBrowser browser;

    public NexusDownloader(string downloadFolder, IReadOnlyList<NexusDownload> downloads,
        AppSettings settings, ILogger? logger, DownloadProgressPool? progressPool,
        CircuitBreaker circuitBreaker, AutoDownloadCefBrowser browser)
    {
        this.downloadFolder = downloadFolder;
        this.downloads = downloads;
        this.maxDownloadSize = settings.MaxDownloadSize * 1024 * 1024;
        bufferSize = settings.BufferSize;
        checkHash = settings.CheckHash;
        discoverExistingFiles = settings.DiscoverExistingFiles;
        timeout = TimeSpan.FromSeconds(settings.HttpTimeout);
        this.logger = logger;
        this.progressPool = progressPool;
        this.circuitBreaker = circuitBreaker;
        this.browser = browser;
        this.client = new HttpClient();
        semaphore = new SemaphoreSlim(settings.MaxConcurrency);
    }

    public async Task DownloadAsync(IProgress<int>? downloadProgress, CancellationToken token)
    {
        if (discoverExistingFiles)
        {
            existingFiles = ScanFolder(downloadFolder);
            logger?.LogInformation("Found {existingFiles.Count} existing files within {downloadFolder.Path}.", existingFiles.Count, downloadFolder);
        }

        // creates a wrapper around the original token so that we can cancel all download tasks
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        var linkedToken = linkedTokenSource.Token;

        var downloadTasks = new List<Task>();
        int pos = 0;
        foreach (var download in downloads)
        {
            int i = Interlocked.Increment(ref pos);
            logger?.LogTrace("File {count}/{total} is queueing for download.", i, downloads.Count);
            // semaphore should not listen for linked token, or the only exception getting thrown will be OperationCanceledException
            await semaphore.WaitAsync(token).ConfigureAwait(false);

            // cancel all remaining downloads if one download fails
            if (linkedToken.IsCancellationRequested)
            {
                break;
            }

            downloadProgress?.Report(i);
            var progress = progressPool?.Get(download);

            downloadTasks.Add(Task.Run(async () =>
            {
                try
                {
                    await circuitBreaker.AutoRetryAsync(async () => await DownloadFileAsync(download, progress?.Progress, linkedToken),
                        static ex =>
                        {
                            // these are most likely user errors. it's useless to retry here
                            if (ex is UnauthorizedAccessException)
                                return true;

                            // protect user's account by respecting TooManyRequests error
                            else if (ex is HttpRequestException httpException && httpException.StatusCode == HttpStatusCode.TooManyRequests)
                                return true;

                            return false;
                        },
                        logger, $"Download for {download.FileName}", linkedToken);
                }
                // catch OperationCancenceledException that bubbles up from pending downloads
                catch (OperationCanceledException)
                {
                    logger?.LogWarning("Download for {downloadName} is cancelled.", download.FileName);
                }
                // cancel all pending downloads if one download fails
                catch
                {
                    linkedTokenSource.Cancel();
                    throw;
                }
                finally
                {
                    progressPool?.Return(progress!); // progress is not null if progress pool is not null
                    semaphore.Release();
                }
            }, linkedToken));
        }

        await Task.WhenAll(downloadTasks);
    }

    /// <summary>
    /// Download a single file and write it to disk
    /// </summary>
    private async Task DownloadFileAsync(NexusDownload download, IProgress<long>? progress, CancellationToken token)
    {
        // skip if file size exceeds size limit
        if (download.FileSize > maxDownloadSize)
        {
            logger?.LogInformation("File {download.FileName} exceeds download limit: {download.FileSize} > {maxDownloadSize}. Skipping ahead.",
                download.FileName, download.FileSize, maxDownloadSize);
            return;
        }
        // skip if file already exists
        // a file exists if its name and size matches the record in wabbajack metadata
        if (existingFiles != null && existingFiles.TryGetValue(download.FileName, out long size) && size == download.FileSize)
        {
            logger?.LogInformation("File {download.FileName} already exists. Skipping ahead.", download.FileName);
            return;
        }

        token.ThrowIfCancellationRequested();

        // acquire download url with timeout
        logger?.LogTrace("Attempting to fetch download Url for {download.Filename}.", download.FileName);
        var url = await browser.GetDownloadUrlAsync(download, token).WaitAsync(timeout, token);
        // acquire file name and file size from header
        string? fileName = default;
        long? fileSize = default;
        using (var headerResponse = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token))
        {
            // attempt to get suggested file name and file size from server
            if (headerResponse.IsSuccessStatusCode)
            {
                fileSize = headerResponse.Content.Headers.ContentLength;
                fileName = headerResponse.Content.Headers.ContentDisposition?.FileName;
            }
        }
        // fall back to file name extraction if there is no suggested file name
        fileName ??= GetFileName(url);

        // sometimes, wabbajack file name does not match actual file name, so we need to check if the file exists again
        if (download.FileName != fileName)
        {
            logger?.LogTrace("File {fileName} does not match wabbajack file name. Checking again if the file already exists.", fileName);

            if (existingFiles != null && existingFiles.TryGetValue(fileName, out var localSize))
            {
                if (fileSize != null && fileSize == localSize)
                {
                    logger?.LogInformation("File {fileName} already exists. Skipping ahead.", fileName);
                    return;
                }
            }
        }

        token.ThrowIfCancellationRequested();

        // download the file and write it to disk
        logger?.LogTrace("Starting download for {file}...", fileName);
        await using var downloadStream = await client.GetStreamAsync(url, token);
        await using var progressStream = new ProgressStream(downloadStream, progress);
        await using var inputStream = new IdleTimeoutStream(progressStream, timeout);
        var filePath = Path.Combine(downloadFolder, fileName);
        await using var fileStream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite);

        if (checkHash)
        {
            var hash = await inputStream.HashingCopy(fileStream, bufferSize, token);
            hash.ThrowOnMismatch(download.Hash, fileName, logger);
            logger?.LogTrace("Verified hash for {file}.", fileName);
        }
        else
            await inputStream.CopyToAsync(fileStream, bufferSize, token);
        await fileStream.FlushAsync(token);

        logger?.LogInformation("Downloaded {file} succesfully.", fileName);
    }

    /// <summary>
    /// Scan the folder for existing files
    /// </summary>
    private Dictionary<string, long> ScanFolder(string folder)
    {
        logger?.LogInformation("Scanning folder {folder} for existing files.", folder);
        var results = new Dictionary<string, long>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly))
        {
            var info = new FileInfo(file);
            var extension = info.Extension.ToLowerInvariant();
            if (acceptedExtensions.Contains(extension))
            {
                var name = info.Name;
                var size = info.Length;
                results.Add(name, size);
                logger?.LogTrace("Discovered file {name} of size {size} B.", name, size);
            }
        }
        return results;
    }

    /// <summary>
    /// Extract file name from a nexus download response
    /// </summary>
    private static string GetFileName(ReadOnlySpan<char> url)
    {
        ReadOnlySpan<char> baseUrl;
        ReadOnlySpan<char> name;

        // separate the base url and query
        var dividerIndex = url.IndexOf('?');
        if (dividerIndex == -1)
            baseUrl = url;
        else
            baseUrl = url[..dividerIndex];

        // look for file name within base url
        dividerIndex = baseUrl.LastIndexOf('/');
        name = baseUrl[(dividerIndex + 1)..];

        return Uri.UnescapeDataString(name.ToString());
    }

    public void Dispose()
    {
        client.Dispose();
        semaphore.Dispose();
        progressPool?.Dispose();
    }
}
