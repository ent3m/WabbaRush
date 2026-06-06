using Microsoft.Win32.SafeHandles;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using WabbajackDownloader.Common.Hashing;
using WabbajackDownloader.Common.Retry;
using WabbajackDownloader.Common.Serialization;
using WabbajackDownloader.Features.WabbajackRepo;

namespace WabbajackDownloader.Features.WabbajackModList;

internal sealed class ModListDownloader(AppSettings settings, RetryHandler<ModListDownloader> retryHandler, ILogger<ModListDownloader> logger)
{
    private const string DefinitionQuery = "/definition.json.gz";
    private const string PartQuery = "/parts/";
    private long _totalBytesDownloaded = 0;

    /// <summary>
    /// Download a wabbajack modlist given its metadata and return the downloaded file path.
    /// </summary>
    public async Task<string> DownloadModListAsync(ModListMetadata metadata, IProgress<long> progress, CancellationToken token)
    {
        logger.LogInformation("Starting download of modlist '{ModListTitle}' from '{DownloadUrl}'.", metadata.Title, metadata.Links.Download);

        // Make sure download folder exists
        var folderPath = Path.Combine(Environment.CurrentDirectory, settings.ModListFolder);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var fileName = GetFileName(metadata.Links.Download);
        var path = Path.Combine(folderPath, fileName);

        token.ThrowIfCancellationRequested();

        // Avoid redownloading if the modlist file is already present and valid
        if (File.Exists(path))
        {
            logger.LogDebug("Found existing file at '{FilePath}'. Verifying hash.", path);
            await using var currentFileStream = File.Open(path, FileMode.Open, FileAccess.Read);
            var currentFileHash = await currentFileStream.Hash(token: token);
            var downloadMetadata = metadata.DownloadMetadata;
            if (downloadMetadata is not null && currentFileHash.Equals(downloadMetadata.Hash))
            {
                logger.LogInformation("Modlist '{ModListTitle}' is already up to date at '{FilePath}'. Skipping download.", metadata.Title, path);
                return path;
            }

            logger.LogInformation("Existing file hash does not match. Re-downloading modlist '{ModListTitle}'.", metadata.Title);
        }

        using var httpClient = CreateHttpClient();
        // Download FileDefinition
        var definition = await GetFileDefinitionAsync(metadata, httpClient, token) ?? throw new InvalidFileDefinitionException("Unable to parse file definition from definition.json.");
        logger.LogDebug("Fetched file definition for '{ModListTitle}': {PartCount} parts, {TotalSize} bytes.", metadata.Title, definition.Parts.Length, definition.Size);

        // Download all parts
        using (var fileHandle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite))
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = settings.MaxConcurrency,
                CancellationToken = token
            };
            await Parallel.ForEachAsync(definition.Parts, options,
                async (part, token) => await retryHandler.AutoRetryAsync(() =>
                DownloadPartAsync(metadata.Links.Download, part, fileHandle, httpClient, progress, token),
                null, token));
        }

        logger.LogDebug("All {PartCount} parts downloaded for '{ModListTitle}'. Verifying file integrity.", definition.Parts.Length, metadata.Title);

        token.ThrowIfCancellationRequested();

        // Check file integrity
        await using var fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
        var hash = await fileStream.Hash(token: token);
        hash.ThrowOnMismatch(definition.Hash, fileName);

        logger.LogInformation("Modlist '{ModListTitle}' downloaded and verified successfully at '{FilePath}'.", metadata.Title, path);
        return path;
    }

    private async Task<FileDefinition?> GetFileDefinitionAsync(ModListMetadata metadata, HttpClient httpClient, CancellationToken token)
    {
        var uri = new Uri(metadata.Links.Download + DefinitionQuery);
        logger.LogDebug("Fetching file definition from {DefinitionUrl}.", uri);
        using var message = BuildMessage(uri);
        using var response = await httpClient.SendAsync(message, token);
        response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync(token);
        await using var gzip = new GZipStream(content, CompressionMode.Decompress);
        var definition = await JsonSerializer.DeserializeAsync<FileDefinition>(gzip, SourceGenerationContext.Default.FileDefinition, token);
        return definition;
    }

    private async Task DownloadPartAsync(string url, PartDefinition part, SafeFileHandle fileHandle, HttpClient httpClient, IProgress<long> progress, CancellationToken token)
    {
        var uri = new Uri(url + PartQuery + part.Index);
        logger.LogTrace("Downloading part {PartIndex} ({PartSize} bytes) from {PartUrl}.", part.Index, part.Size, uri);
        using var message = BuildMessage(uri);
        using var response = await httpClient.SendAsync(message, token);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsByteArrayAsync(token);

        await using var stream = new MemoryStream(content);
        var hash = await stream.Hash(token: token);
        hash.ThrowOnMismatch(part.Hash, $"part {part.Index}");

        await RandomAccess.WriteAsync(fileHandle, content, part.Offset, token);
        progress.Report(Interlocked.Add(ref _totalBytesDownloaded, content.Length));
    }

    private static string GetFileName(string downloadLink)
    {
        var link = Uri.UnescapeDataString(downloadLink);
        var begin = link.LastIndexOf('/') + 1;
        var end = link.LastIndexOf('_');
        return link[begin..end];
    }

    private static HttpRequestMessage BuildMessage(Uri uri)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, uri);
        message.Headers.Host = uri.Host;
        message.Headers.Accept.TryParseAdd("*/*");
        message.Headers.AcceptEncoding.TryParseAdd("deflate, gzip");
        return message;
    }

    // IPv4 HttpClient that works with Wabbajack CDN
    private HttpClient CreateHttpClient()
    {
        // Force IPv4 because Wabbajack CDN has a misconfigured IPv6 at the time of writing this code
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = static async (context, token) =>
            {
                // Use DNS to look up the IP addresses of the target host:
                // - IP v4: AddressFamily.InterNetwork
                // - IP v6: AddressFamily.InterNetworkV6
                // - IP v4 or IP v6: AddressFamily.Unspecified
                // note: this method throws a SocketException when there is no IP address for the host
                var entry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host, AddressFamily.InterNetwork, token);
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    // Turn off Nagle's algorithm since it degrades performance in most HttpClient scenarios.
                    NoDelay = true
                };
                try
                {
                    await socket.ConnectAsync(entry.AddressList, context.DnsEndPoint.Port, token);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(settings.Timeout)
        };
    }
}
