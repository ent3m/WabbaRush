using Microsoft.Win32.SafeHandles;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using WabbajackDownloader.Common.Configuration;
using WabbajackDownloader.Common.Hashing;
using WabbajackDownloader.Common.Retry;
using WabbajackDownloader.Common.Serialization;
using WabbajackDownloader.Features.WabbajackRepo;

namespace WabbajackDownloader.Features.WabbajackModList;

public class ModListDownloader : IDisposable
{
    private readonly AppSettings _settings;
    private readonly RetryHandler<ModListDownloader> _circuitBreaker;
    private readonly JsonSerializerOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ModListDownloader> _logger;

    private const string definitionQuery = "/definition.json.gz";
    private const string partQuery = "/parts/";

    public ModListDownloader(AppSettings settings, RetryHandler<ModListDownloader> circuitBreaker, SerializerOptions serializerOptions, ILogger<ModListDownloader> logger)
    {
        _settings = settings;
        _circuitBreaker = circuitBreaker;
        _options = serializerOptions.Options;
        _logger = logger;

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

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(settings.Timeout)
        };
    }

    public async Task<string> DownloadModListAsync(ModListMetadata metadata, IProgress<long>? progress, CancellationToken token)
    {
        _logger.LogInformation("Starting download of mod list '{ModListTitle}' from {DownloadUrl}.", metadata.Title, metadata.Links.Download);

        // Make sure download folder exists
        var folderPath = Path.Combine(Environment.CurrentDirectory, _settings.ModListFolder);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var fileName = GetFileName(metadata.Links.Download);
        var path = Path.Combine(folderPath, fileName);

        token.ThrowIfCancellationRequested();

        // Avoid redownloading if the mod list file is already present and valid
        if (File.Exists(path))
        {
            _logger.LogDebug("Found existing file at {FilePath}. Verifying hash.", path);
            await using var currentFileStream = File.Open(path, FileMode.Open, FileAccess.Read);
            var currentFileHash = await currentFileStream.Hash(_settings.BufferSize, token);
            var downloadMetadata = metadata.DownloadMetadata;
            if (downloadMetadata is not null && currentFileHash.Equals(downloadMetadata.Hash))
            {
                _logger.LogInformation("Mod list '{ModListTitle}' is already up to date at {FilePath}. Skipping download.", metadata.Title, path);
                return path;
            }

            _logger.LogInformation("Existing file hash does not match. Re-downloading mod list '{ModListTitle}'.", metadata.Title);
        }

        // Download FileDefinition
        var definition = await GetFileDefinitionAsync(metadata, token) ?? throw new InvalidFileDefinitionException("Unable to parse file definition from definition.json.");
        _logger.LogInformation("Fetched file definition for '{ModListTitle}': {PartCount} parts, {TotalSize} bytes.", metadata.Title, definition.Parts.Length, definition.Size);

        // Download all parts
        using (var fileHandle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite))
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = _settings.MaxConcurrency,
                CancellationToken = token
            };
            await Parallel.ForEachAsync(definition.Parts, options,
                async (part, token) => await _circuitBreaker.AutoRetryAsync(() =>
                DownloadPartAsync(metadata.Links.Download, part, fileHandle, progress, token),
                null, token));
        }

        _logger.LogDebug("All {PartCount} parts downloaded for '{ModListTitle}'. Verifying file integrity.", definition.Parts.Length, metadata.Title);

        token.ThrowIfCancellationRequested();

        // Check file integrity
        await using var fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
        var hash = await fileStream.Hash(_settings.BufferSize, token);
        hash.ThrowOnMismatch(definition.Hash, fileName);

        _logger.LogInformation("Mod list '{ModListTitle}' downloaded and verified successfully at {FilePath}.", metadata.Title, path);
        return path;
    }

    private async Task<FileDefinition?> GetFileDefinitionAsync(ModListMetadata metadata, CancellationToken token)
    {
        var uri = new Uri(metadata.Links.Download + definitionQuery);
        _logger.LogDebug("Fetching file definition from {DefinitionUrl}.", uri);
        using var message = BuildMessage(uri);
        using var response = await _httpClient.SendAsync(message, token);
        response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync(token);
        await using var gzip = new GZipStream(content, CompressionMode.Decompress);
        var definition = await JsonSerializer.DeserializeAsync<FileDefinition>(gzip, _options, token);
        return definition;
    }

    private async Task DownloadPartAsync(string url, PartDefinition part, SafeFileHandle fileHandle, IProgress<long>? progress, CancellationToken token)
    {
        var uri = new Uri(url + partQuery + part.Index);
        _logger.LogDebug("Downloading part {PartIndex} ({PartSize} bytes) from {PartUrl}.", part.Index, part.Size, uri);
        using var message = BuildMessage(uri);
        using var response = await _httpClient.SendAsync(message, token);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsByteArrayAsync(token);

        await using var stream = new MemoryStream(content);
        var hash = await stream.Hash(_settings.BufferSize, token);
        hash.ThrowOnMismatch(part.Hash, $"part {part.Index}");

        await RandomAccess.WriteAsync(fileHandle, content, part.Offset, token);
        progress?.Report(content.Length);
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
        _httpClient.Dispose();
    }
}
