using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WabbajackDownloader.ModList;
using WabbajackDownloader.Serializer;

namespace WabbajackDownloader.Core;

internal class RepositoriesDownloader
{
    private const string repositoriesUrl = "https://raw.githubusercontent.com/wabbajack-tools/mod-lists/master/repositories.json";

    public static async Task<ModListMetadata[]?> FetchRepositoriesAsync(int maxConcurrency, int timeout, ILogger? logger, CancellationToken token)
    {
        ModListMetadata[]? repositories = default;
        try
        {
            logger?.LogTrace("Downloading repositories.json from {url}.", repositoriesUrl);
            using var client = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(timeout)
            };
            var repo = await client.GetFromJsonAsync<Dictionary<string, Uri>>(repositoriesUrl, SerializerOptions.Options, token);
            logger?.LogTrace("Extracting mod lists from repositories.json.");
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = maxConcurrency,
                CancellationToken = token
            };
            var lists = new ConcurrentBag<ModListMetadata>();
            await Parallel.ForEachAsync(repo!, options, Fetch);
            repositories = lists.OrderBy(t => t.Title).ToArray();
            logger?.LogInformation("Found {count} mod lists within repositories.", repositories.Length);

            async ValueTask Fetch(KeyValuePair<string, Uri> item, CancellationToken token)
            {
                var data = await client.GetFromJsonAsync<ModListMetadata[]>(item.Value, SerializerOptions.Options, token);
                if (data != null)
                {
                    foreach (var entry in data)
                    {
                        lists.Add(entry);
                        logger?.LogTrace("Added {title} to available mod lists.", entry.Title);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to fetch mod list repositories.");
        }
        return repositories;
    }
}
