using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    private readonly Dictionary<string, ulong> existingFiles;
    private static readonly string[] acceptedExtensions = [".zip", ".rar", ".7z"];
    private readonly HttpClient fetchClient;
    private readonly HttpClient downloadClient;

    public NexusDownloader(IStorageFolder downloadFolder, List<NexusDownload> downloads, CookieContainer cookieContainer, ulong maxDownloadSize)
    {
        this.downloadFolder = downloadFolder;
        this.downloads = downloads;
        this.maxDownloadSize = maxDownloadSize;
        existingFiles = [];
        HttpClientHandler handler = new()
        {
            CookieContainer = cookieContainer,
            UseCookies = true
        };
        fetchClient = new HttpClient(handler, true);
        downloadClient = new HttpClient();
    }

    public async Task DownloadFilesAsync(int position, CancellationToken token)
    {
        if (App.Settings.DiscoverExistingFiles)
        {
            await ScanDownloadFolder(downloadFolder, acceptedExtensions, existingFiles);
#if DEBUG
            Debug.WriteLine($"Found {existingFiles.Count} existing files within {downloadFolder.Path}");
#endif
        }

        for (int i = position; i < downloads.Count; i++)
        {
            Position = i;
            var download = downloads[i];
            OnDownloading(i, download.FileName, download.FileSize);

            // skip if file size exceeds size limit or if file already exists
            if (download.FileSize > maxDownloadSize)
            {
#if DEBUG
                Debug.WriteLine($"File {download.FileName} exceeds download limit: {download.FileSize} > {maxDownloadSize}. Skipping ahead.");
#endif
                continue;
            }

            if (existingFiles.TryGetValue(download.FileName, out ulong size) && size == download.FileSize)
            {
#if DEBUG
                Debug.WriteLine($"File {download.FileName} already exists. Skipping ahead.");
#endif
                continue;
            }

            // construct and send POST request to acquire download url
            token.ThrowIfCancellationRequested();
            var request = ConstructRequest(download);
            var response = await request.SendAsync(fetchClient, true, token);
#if DEBUG
            var responseString = await response.ReadAsStringAsync(token);
            Debug.WriteLine($"HTTP Response Content: {responseString}");
#endif
            // interpret download url and extract file name
            var json = JsonNode.Parse(response.ReadAsStream(token))?.AsObject()
                ?? throw new InvalidHttpReponseException($"Unable to parse Http response as JsonObject.");
            var url = json["url"]?.ToString()
                ?? throw new InvalidHttpReponseException($"Http response does not contain download url: {json}");

            var fileName = GetDownloadUrl(url);
            if (download.FileName != fileName)
            {
#if DEBUG
                Debug.WriteLine($"File {fileName} does not match wabbajack file name. Checking again.");
#endif
                // sometimes, wabbajack file name does not match actual file name, so we need to check if the file exists again
                // if that is the case, avoid wasting bandwidth and skip to the next download
                if (existingFiles.ContainsKey(fileName))
                {
#if DEBUG
                    Debug.WriteLine($"File {fileName} already exists. Skipping download.");
#endif
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
            existingFiles[download.FileName] = download.FileSize;
#if DEBUG
            Debug.WriteLine($"Downloaded file '{fileName}' succesfully.");
#endif
        }
    }

    private void OnDownloading(int position, string fileName, ulong size) => Downloading?.Invoke(position, fileName, size);

    /// <summary>
    /// Scan the folder for existing files
    /// </summary>
    private static async Task ScanDownloadFolder(IStorageFolder folder, string[] acceptedExtensions, Dictionary<string, ulong> result)
    {
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
                    result.Add(name, size);
                }
            }
        }
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
    private static string GetDownloadUrl(ReadOnlySpan<char> url)
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
