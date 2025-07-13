using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using WabbajackDownloader.Common;
using WabbajackDownloader.Configuration;
using WabbajackDownloader.Exceptions;
using WabbajackDownloader.Extensions;
using WabbajackDownloader.Hashing;
using WabbajackDownloader.Http;

namespace WabbajackDownloader.Core;

internal class NexusDownloader : IDisposable
{
    private static readonly string[] acceptedExtensions = [".zip", ".rar", ".7z"];

    private readonly string downloadFolder;
    private readonly IReadOnlyList<NexusDownload> downloads;
    private readonly long maxDownloadSize;
    private readonly int bufferSize;
    private readonly bool checkHash;
    private readonly string userAgent;
    private readonly bool discoverExistingFiles;
    private readonly TimeSpan timeout;
    private readonly ILogger? logger;
    private readonly DownloadProgressPool? progressPool;
    private readonly CircuitBreaker circuitBreaker;

    private Dictionary<string, long>? existingFiles;
    private readonly HttpClientHandler handler;
    private readonly SemaphoreSlim semaphore;

    public NexusDownloader(string downloadFolder, IReadOnlyList<NexusDownload> downloads, CookieContainer cookieContainer,
        AppSettings settings, ILogger? logger, DownloadProgressPool? progressPool, CircuitBreaker circuitBreaker)
    {
        this.downloadFolder = downloadFolder;
        this.downloads = downloads;
        this.maxDownloadSize = settings.MaxDownloadSize * 1024 * 1024;
        bufferSize = settings.BufferSize;
        checkHash = settings.CheckHash;
        userAgent = settings.UserAgent;
        discoverExistingFiles = settings.DiscoverExistingFiles;
        timeout = TimeSpan.FromSeconds(settings.HttpTimeout);
        this.logger = logger;
        this.progressPool = progressPool;
        this.circuitBreaker = circuitBreaker;

        handler = new HttpClientHandler()
        {
            CookieContainer = cookieContainer,
            UseCookies = true
        };
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
                            if (ex is InvalidJsonResponseException or UnauthorizedAccessException)
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
        if (existingFiles != null && existingFiles.TryGetValue(download.FileName, out long size) && size == download.FileSize)
        {
            logger?.LogInformation("File {download.FileName} already exists. Skipping ahead.", download.FileName);
            return;
        }

        token.ThrowIfCancellationRequested();

        // construct and send POST request to acquire download url
        using var client = new HttpClient(handler, false)
        {
            Timeout = timeout
        };
        client.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent);

        logger?.LogTrace("Sending HTTP POST request for {file}.", download.FileName);
        var request = ConstructRequest(download);
        var content = await request.SendAsync(client, true, token);
        var contentString = await content.ReadAsStringAsync(token);

        // interpret download url and extract file name
        string fileName;
        string url;
        try
        {
            var jsonResponse = JsonNode.Parse(contentString)!.AsObject();
            url = jsonResponse["url"]!.ToString();
            fileName = GetFileName(url);
        }
        catch (Exception ex)
        {
            throw new InvalidJsonResponseException($"Http response does not contain download url: {contentString}", ex);
        }

        // sometimes, wabbajack file name does not match actual file name, so we need to check if the file exists again
        if (download.FileName != fileName)
        {
            logger?.LogTrace("File {fileName} does not match wabbajack file name. Checking again if the file already exists.", fileName);

            if (existingFiles != null && existingFiles.ContainsKey(fileName))
            {
                // there is no file size, so we have to fetch file size from download url
                using var headerResponse = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
                var sizeFromHeader = headerResponse.Content.Headers.ContentLength;
                if (sizeFromHeader != null && sizeFromHeader == download.FileSize)
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
    /// Construct a HTTP POST request for www.nexusmods.com
    /// </summary>
    private static HttpRequest ConstructRequest(NexusDownload download)
    {
        var request = new HttpRequest()
        {
            Method = HttpMethod.Post,
            BaseUrl = "https://www.nexusmods.com/Core/Libs/Common/Managers/Downloads",
            Query = new()
            {
                ["GenerateDownloadUrl"] = string.Empty
            },
            Content = new()
            {
                Type = ContentType.FormUrl,
                ["fid"] = download.FileID,
                ["game_id"] = download.GameID
            },
            ["origin"] = "https://www.nexusmods.com"
        };
        return request;
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
        handler.Dispose();
        semaphore.Dispose();
        progressPool?.Dispose();
    }
}
