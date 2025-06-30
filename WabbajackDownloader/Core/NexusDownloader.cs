using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using WabbajackDownloader.Exceptions;
using WabbajackDownloader.Http;

namespace WabbajackDownloader.Core;

internal class NexusDownloader : IDisposable
{
    public event Action<int, string, ulong>? Downloading;
    public int Position { get; private set; }

    private readonly IStorageFolder downloadFolder;
    private readonly List<NexusDownload> downloads;

    private readonly ulong maxDownloadSize;
    private readonly int bufferSize;
    private readonly int minRetryDelay;
    private readonly int maxRetryDelay;
    private readonly bool checkHash;

    private readonly HttpClient fetchClient;
    private readonly HttpClient[] downloadClients;
    private readonly HttpClient downloadClient; // obsolete
    private readonly Random random = new();
    private Dictionary<string, ulong>? existingFiles;

    private static readonly string[] acceptedExtensions = [".zip", ".rar", ".7z"];

    public NexusDownloader(IStorageFolder downloadFolder, List<NexusDownload> downloads, CookieContainer cookieContainer)
    {
        maxDownloadSize = (ulong)App.Settings.MaxDownloadSize * 1024 * 1024;
        bufferSize = App.Settings.BufferSize;
        minRetryDelay = App.Settings.MinRetryDelay;
        maxRetryDelay = App.Settings.MaxRetryDelay;
        checkHash = App.Settings.CheckHash;
        var maxConcurrent = App.Settings.MaxConcurrentDownload;

        this.downloadFolder = downloadFolder;
        this.downloads = downloads;
        HttpClientHandler handler = new()
        {
            CookieContainer = cookieContainer,
            UseCookies = true
        };
        fetchClient = new HttpClient(handler, true);

        downloadClient = new HttpClient(); // obsolete
        downloadClients = new HttpClient[maxConcurrent];
        for (int i = 0; i < maxConcurrent; i++)
        {
            downloadClients[i] = new HttpClient();
        }
    }

    public async Task DownloadFilesAsync(int position, CancellationToken token)
    {
        if (App.Settings.DiscoverExistingFiles)
        {
            existingFiles = await ScanDownloadFolder(downloadFolder, acceptedExtensions);
            App.Logger.LogInformation("Found {existingFiles.Count} existing files within {downloadFolder.Path}.", existingFiles.Count, downloadFolder.Path);
        }

        for (int i = position; i < downloads.Count; i++)
        {
            Position = i;
            var download = downloads[i];
            OnDownloading(i, download.FileName, download.FileSize);

            // skip if file size exceeds size limit or if file already exists
            if (download.FileSize > maxDownloadSize)
            {
                App.Logger.LogTrace("File {download.FileName} exceeds download limit: {download.FileSize} > {maxDownloadSize}. Skipping ahead.", 
                    download.FileName, download.FileSize, maxDownloadSize);
                continue;
            }

            if (existingFiles != null && existingFiles.TryGetValue(download.FileName, out ulong size) && size == download.FileSize)
            {
                App.Logger.LogTrace("File {download.FileName} already exists. Skipping ahead.", download.FileName);
                continue;
            }

            // construct and send POST request to acquire download url
            token.ThrowIfCancellationRequested();
            var request = ConstructRequest(download);
            var response = await request.SendAsync(fetchClient, true, token);

            App.Logger.LogInformation("HTTP Response Content: {responseString}", response.ReadAsStringAsync(token).GetAwaiter().GetResult());

            // interpret download url and extract file name
            var jsonNode = await JsonNode.ParseAsync(response.ReadAsStream(token), cancellationToken: token)
                ?? throw new InvalidHttpReponseException($"Unable to parse Http response as JsonNode.");
            var json = jsonNode?.AsObject()
                ?? throw new InvalidHttpReponseException($"Unable to parse Http response as JsonObject.");
            var url = json["url"]?.ToString()
                ?? throw new InvalidHttpReponseException($"Http response does not contain download url: {json}");

            var fileName = GetFileName(url);

            // sometimes, wabbajack file name does not match actual file name, so we need to check if the file exists again
            // if the file already exists, avoid wasting bandwidth and skip to the next download
            if (download.FileName != fileName)
            {
                App.Logger.LogTrace("File {fileName} does not match wabbajack file name. Checking again.", fileName);

                if (existingFiles != null && existingFiles.ContainsKey(fileName))
                {
                    App.Logger.LogTrace("File {fileName} already exists. Skipping ahead.", fileName);
                    continue;
                }
            }

            // download the file and write it to disk
            token.ThrowIfCancellationRequested();
            using (var downloadStream = await downloadClient.GetStreamAsync(url, token))
            {
                using var file = await downloadFolder.CreateFileAsync(fileName) ?? throw new UnauthorizedAccessException("Unable to create file in download folder.");
                using var fileStream = await file.OpenWriteAsync();
                await downloadStream.CopyToAsync(fileStream, token);
                await fileStream.FlushAsync(token);
            }

            App.Logger.LogInformation("Downloaded file '{fileName}' succesfully.", fileName);
        }
    }

    private void OnDownloading(int position, string fileName, ulong size) => Downloading?.Invoke(position, fileName, size);

    /// <summary>
    /// Scan the folder for existing files
    /// </summary>
    private static async Task<Dictionary<string, ulong>> ScanDownloadFolder(IStorageFolder folder, string[] acceptedExtensions)
    {
        var results = new Dictionary<string, ulong>();
        var items = folder.GetItemsAsync();
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
                    results.Add(name, size);
                }
            }
        }
        return results;
    }

    /// <summary>
    /// Construct a HTTP POST request for nexusmods.com
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
        if (dividerIndex == 1)
            throw new InvalidHttpReponseException($"Unable to extract file name from url: {url}");
        else
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
        Downloading = null;
        downloadClient.Dispose();
        fetchClient.Dispose();
    }
}
