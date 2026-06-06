using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using WabbajackDownloader.Common.Retry;
using WabbajackDownloader.Common.Serialization;

namespace WabbajackDownloader.Features.WabbajackRepo;

internal sealed class RepositoriesDownloader(AppSettings settings, ILogger<RepositoriesDownloader> logger, RetryHandler<RepositoriesDownloader> retryHandler)
{
    public ModListMetadata[]? Repositories { get; private set; }

    private const string repositoriesUrl = "https://raw.githubusercontent.com/wabbajack-tools/mod-lists/master/repositories.json";

    public async Task FetchRepositoriesAsync(CancellationToken token)
    {
        logger.LogInformation("Fetching repository metadata from {RepositoriesUrl}.", repositoriesUrl);

        using var client = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(settings.Timeout)
        };

        try
        {
            var repo = await client.GetFromJsonAsync<Dictionary<string, Uri>>(repositoriesUrl, SourceGenerationContext.Default.DictionaryStringUri, token);
            if (repo is null)
            {
                logger.LogWarning("Repositories at {Url} return no metadata.", repositoriesUrl);
                return;
            }

            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = settings.MaxConcurrency,
                CancellationToken = token
            };
            var lists = new ConcurrentBag<ModListMetadata>();

            await Parallel.ForEachAsync(repo, options, (kvp, ct) => new ValueTask(retryHandler.AutoRetryAsync(() => Fetch(kvp, ct), null, ct)));
            Repositories = lists.OrderBy(t => t.Title).ToArray();
            logger.LogInformation("Loaded {ModListCount} modlists from {RepositoryCount} repositories.", Repositories.Length, repo.Count);

            async Task Fetch(KeyValuePair<string, Uri> item, CancellationToken token)
            {
                try
                {
                    var metadata = await client.GetFromJsonAsync<ModListMetadata[]>(item.Value, SourceGenerationContext.Default.ModListMetadataArray, token);
                    if (metadata is not null)
                    {
                        foreach (var entry in metadata)
                        {
                            lists.Add(entry);
                            logger.LogTrace("Added '{ModListTitle}' from {Url}.", entry.Title, item.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Retry on TooManyRequests or a transient error. Throw again for the retry handler to catch.
                    if (ex is HttpRequestException)
                        throw;
                    // Retry on Http Timeout. Throw a different exception for the retry handler to catch.
                    else if (ex is TaskCanceledException && ex.Message.Contains("HttpClient.Timeout"))
                        throw new Exception(ex.Message);

                    logger.LogWarning(ex, "Failed to fetch ModListMetadata from {Repository} at {Url}. Skipping.", item.Key, item.Value);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch repository metadata from {RepositoriesUrl}.", repositoriesUrl);
            throw;
        }
    }
}