using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using WabbajackDownloader.Exceptions;
using WabbajackDownloader.Extensions;
using WabbajackDownloader.Hashing;
using WabbajackDownloader.Http;

namespace WabbajackDownloader.Core;

internal class NexusDownloader : IDisposable
{
    private static readonly string[] acceptedExtensions = [".zip", ".rar", ".7z"];

    private readonly IStorageFolder downloadFolder;
    private readonly IReadOnlyList<NexusDownload> downloads;
    private readonly long maxDownloadSize;
    private readonly int bufferSize;
    private readonly int maxRetries;
    private readonly int minRetryDelay;
    private readonly int maxRetryDelay;
    private readonly bool checkHash;
    private readonly string userAgent;
    private readonly bool discoverExistingFiles;
    private readonly ILogger? logger;
    private readonly IProgress<int>? downloadProgress;
    private readonly ProgressPool? progressPool;

    private readonly Random random = new();
    private Dictionary<string, long>? existingFiles;
    private readonly HttpClientHandler handler;
    private readonly SemaphoreSlim semaphore;

    public NexusDownloader(IStorageFolder downloadFolder, IReadOnlyList<NexusDownload> downloads, CookieContainer cookieContainer,
        int maxDownloadSize, int bufferSize, int maxRetries, int minRetryDelay, int maxRetryDelay, bool checkHash,
        int maxConcurrentDownload, string userAgent, bool discoverExistingFiles, ILogger? logger,
        IProgress<int>? downloadProgress, ProgressPool? progressPool)
    {
        this.downloadFolder = downloadFolder;
        this.downloads = downloads;
        this.maxDownloadSize = maxDownloadSize * 1024 * 1024;
        this.bufferSize = bufferSize;
        this.maxRetries = maxRetries;
        this.minRetryDelay = minRetryDelay;
        this.maxRetryDelay = maxRetryDelay;
        this.checkHash = checkHash;
        this.userAgent = userAgent;
        this.discoverExistingFiles = discoverExistingFiles;
        this.logger = logger;
        this.downloadProgress = downloadProgress;
        this.progressPool = progressPool;

        handler = new HttpClientHandler()
        {
            CookieContainer = cookieContainer,
            UseCookies = true
        };
        semaphore = new SemaphoreSlim(maxConcurrentDownload);
    }

    public async Task DownloadAsync(CancellationToken token)
    {
        if (discoverExistingFiles)
        {
            existingFiles = await ScanFolderAsync(downloadFolder).ConfigureAwait(false);
            logger?.LogInformation("Found {existingFiles.Count} existing files within {downloadFolder.Path}.", existingFiles.Count, downloadFolder.Path);
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
            // semaphore should not listen for linked token, otherwise the only exception getting thrown will be OperationCanceledException
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
                    await DownloadFileWithRetryAsync(download, progress?.Progress, linkedToken);
                }
                // catch OperationCancenceledException that bubbles up from pending downloads
                catch (OperationCanceledException)
                {
                    logger?.LogWarning("Download {downloadName} is cancelled.", download.FileName);
                    throw;
                }
                // cancel all pending downloads if one download fails
                catch
                {
                    linkedTokenSource.Cancel();
                    throw;
                }
                finally
                {
                    progressPool?.Return(progress!); // if progressPool is not null then progress won't be null
                    semaphore.Release();
                }
            }, linkedToken));
        }

        await Task.WhenAll(downloadTasks);
    }

    /// <summary>
    /// Download file with error handling and retry
    /// </summary>
    private async Task DownloadFileWithRetryAsync(NexusDownload download, IProgress<long>? progress, CancellationToken token)
    {
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                await DownloadFileAsync(download, progress, token);
                break;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // these are most likely user errors. it's useless to retry here
                if (ex is InvalidJsonResponseException or UnauthorizedAccessException)
                {
                    throw;
                }
                // protect user's account by respecting TooManyRequests error
                else if (ex is HttpRequestException httpException && httpException.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    throw;
                }
                // retry if there are attempts remaining
                if (retryCount < maxRetries)
                {
                    retryCount++;
                    var delay = random.Next(minRetryDelay, maxRetryDelay);
                    logger?.LogWarning(ex.GetBaseException(), "Download failed for file {file}. Attempting {retryCount} retry in {delay} milliseconds.", download.FileName, retryCount.DisplayWithSuffix(), delay);
                    await Task.Delay(delay, token);
                }
                else
                {
                    logger?.LogError(ex.GetBaseException(), "Download failed for file {file}. No retries remaining.", download.FileName);
                    throw;
                }
            }
        }
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
        using var client = new HttpClient(handler, false);
        client.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent);

        logger?.LogTrace("Sending HTTP POST request for file {file}.", download.FileName);
        var request = ConstructRequest(download);
        var response = await request.SendAsync(client, true, token);
        var responseString = await response.ReadAsStringAsync(token);

        // interpret download url and extract file name
        string fileName;
        string url;
        try
        {
            var jsonResponse = JsonNode.Parse(responseString)!.AsObject();
            url = jsonResponse["url"]!.ToString();
            fileName = GetFileName(url);
        }
        catch (Exception ex)
        {
            throw new InvalidJsonResponseException($"Http response does not contain download url: {responseString}", ex);
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
        logger?.LogTrace("Starting download for {file}...", download.FileName);
        using var downloadStream = await client.GetStreamAsync(url, token);
        using var inputStream = new ProgressStream(downloadStream, progress);
        using var file = await downloadFolder.CreateFileAsync(fileName) ?? throw new UnauthorizedAccessException("Unable to create file in download folder.");
        using var fileStream = await file.OpenWriteAsync();
        if (checkHash)
        {
            var hash = await inputStream.HashingCopy(fileStream, token, bufferSize);
            if (!hash.Equals(download.Hash))
            {
                logger?.LogError("Hash does not match pre-computed value for {fileName}.", fileName);
                throw new HashMismatchException($"Hash does not match pre-computed value for {fileName}.\nExpected: {download.Hash}\nComputed: {hash}");
            }
            else
                logger?.LogTrace("Verified hash for file {file}.", fileName);
        }
        else
            await inputStream.CopyToAsync(fileStream, bufferSize, token);
        await fileStream.FlushAsync(token);

        logger?.LogInformation("Downloaded file {fileName} succesfully.", fileName);
    }

    /// <summary>
    /// Scan the folder for existing files
    /// </summary>
    private async Task<Dictionary<string, long>> ScanFolderAsync(IStorageFolder folder)
    {
        var results = new Dictionary<string, long>();
        var items = folder.GetItemsAsync();
        logger?.LogTrace("Scanning folder {folder} for existing files.", folder.Name);
        await foreach (var item in items)
        {
            if (item is IStorageFile file)
            {
                var name = file.Name;
                var extension = GetFileExtension(name).ToLowerInvariant();
                if (acceptedExtensions.Contains(extension))
                {
                    var properties = await file.GetBasicPropertiesAsync();
                    var size = properties.Size ?? 0;
                    results.Add(name, (long)size);
                    logger?.LogTrace("Discovered file {name} with size {size}B.", name, size);
                }
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

    /// <summary>
    /// Extract extension from file name
    /// </summary>
    private static string GetFileExtension(ReadOnlySpan<char> fileName)
    {
        var divider = fileName.LastIndexOf('.');
        if (divider == -1)
            return string.Empty;
        else
            return fileName[divider..].ToString();
    }

    public void Dispose()
    {
        handler.Dispose();
        semaphore.Dispose();
    }
}
