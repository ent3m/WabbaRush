using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WabbajackDownloader.Common;
using WabbajackDownloader.Configuration;
using WabbajackDownloader.Exceptions;
using WabbajackDownloader.Extensions;
using WabbajackDownloader.Hashing;
using WabbajackDownloader.ModList;
using WabbajackDownloader.Serializer;

namespace WabbajackDownloader.Core;

internal class ModListDownloader : IDisposable
{
    private readonly string downloadFolder;
    private readonly int bufferSize;
    private readonly int maxConcurrency;
    private readonly bool discoverExistingFiles;
    private readonly bool checkHash;
    private readonly CircuitBreaker circuitBreaker;
    private readonly ILogger? logger;
    private readonly HttpClient client;

    private const string definitionQuery = "/definition.json.gz";
    private const string partQuery = "/parts/";

    public ModListDownloader(AppSettings settings, CircuitBreaker circuitBreaker, ILogger? logger)
    {
        this.downloadFolder = settings.ModListDownloadPath;
        this.bufferSize = settings.BufferSize;
        this.maxConcurrency = settings.MaxConcurrency;
        this.discoverExistingFiles = settings.DiscoverExistingFiles;
        this.checkHash = settings.CheckHash;
        this.circuitBreaker = circuitBreaker;
        this.logger = logger;

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

        client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(settings.HttpTimeout)
        };
    }

    public async Task<string> DownloadWabbajackAsync(ModListMetadata metadata, IProgress<long>? progress, CancellationToken token)
    {
        // make sure download folder exists
        var folderPath = Path.Combine(Environment.CurrentDirectory, downloadFolder);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var fileName = GetFileName(metadata.Links.Download);
        var path = Path.Combine(folderPath, fileName);

        token.ThrowIfCancellationRequested();
        logger?.LogTrace("Starting download process for {fileName}.", fileName);

        // check for existing file
        if (discoverExistingFiles && File.Exists(path))
        {
            logger?.LogTrace("Discovered existing file. Checking file integrity.");
            await using var readStream = File.Open(path, FileMode.Open, FileAccess.Read);
            var hash = await readStream.Hash(bufferSize, token);
            if (hash.Equals(metadata.DownloadMetadata.Hash))
            {
                logger?.LogInformation("Existing wabbajack file discovered and verified. Skipping download.");
                return path;
            }
            else
                logger?.LogTrace("Existing wabbajack file is either corrupted or oudated. Proceeding to download.");
        }

        // get file and part definitions
        var definition = await GetFileDefinitionAsync(metadata, token) ?? throw new InvalidJsonResponseException("Unable to parse file definition from definition.json.");

        // download all parts
        using (var fileHandle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite))
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                CancellationToken = token
            };
            await Parallel.ForEachAsync(definition.Parts, options,
                async (part, token) => await circuitBreaker.AutoRetryAsync(
                    async () => await DownloadPartAsync(metadata.Links.Download, part, fileHandle, progress, token),
                    null, logger, "Part download", token));
        }

        token.ThrowIfCancellationRequested();

        // check file integrity
        if (checkHash)
        {
            await using var fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
            var hash = await fileStream.Hash(bufferSize, token);
            hash.ThrowOnMismatch(definition.Hash, fileName, logger);
            logger?.LogTrace("Verified hash for {file}.", fileName);
        }

        logger?.LogInformation("Downloaded {file} succesfully.", fileName);

        return path;
    }

    private async Task<FileDefinition?> GetFileDefinitionAsync(ModListMetadata metadata, CancellationToken token)
    {
        logger?.LogTrace("Getting FileDefinition from definition.json.gz.");
        var uri = new Uri(metadata.Links.Download + definitionQuery);
        using var message = BuildMessage(uri);
        using var response = await client.SendAsync(message, token);
        response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync(token);
        await using var gzip = new GZipStream(content, CompressionMode.Decompress);
        var definition = await JsonSerializer.DeserializeAsync<FileDefinition>(gzip, SerializerOptions.Options, token);
        logger?.LogTrace("FileDefinition successfully retrieved.");
        return definition;
    }

    private async Task DownloadPartAsync(string url, PartDefinition part, SafeFileHandle fileHandle, IProgress<long>? progress, CancellationToken token)
    {
        logger?.LogTrace("Downloading part {index}.", part.Index);
        var uri = new Uri(url + partQuery + part.Index);
        using var message = BuildMessage(uri);
        using var response = await client.SendAsync(message, token);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsByteArrayAsync(token);
        if (checkHash)
        {
            await using var stream = new MemoryStream(content);
            var hash = await stream.Hash(bufferSize, token);
            hash.ThrowOnMismatch(part.Hash, $"part {part.Index}", logger);
            logger?.LogTrace("Verified part {index}.", part.Index);
        }
        await RandomAccess.WriteAsync(fileHandle, content, part.Offset, token);
        progress?.Report(content.Length);
        logger?.LogTrace("Part {index} written to disk.", part.Index);
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

    public void Dispose()
    {
        client.Dispose();
    }
}
