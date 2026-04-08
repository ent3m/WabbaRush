using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using WabbajackDownloader.Common.Configuration;
using WabbajackDownloader.Common.Retry;
using WabbajackDownloader.Common.Serialization;

namespace WabbajackDownloader.Features.WabbajackRepo;

public class RepositoriesDownloader(AppSettings settings, SerializerOptions serializerOptions, ILogger<RepositoriesDownloader> logger, RetryHandler<RepositoriesDownloader> retryHandler)
{
    public ModListMetadata[]? Repositories { get; private set; }

    private const string repositoriesUrl = "https://raw.githubusercontent.com/wabbajack-tools/mod-lists/master/repositories.json";
    private readonly AppSettings _settings = settings;
    private readonly JsonSerializerOptions _options = serializerOptions.Options;
    private readonly ILogger<RepositoriesDownloader> _logger = logger;
    private readonly RetryHandler<RepositoriesDownloader> _retryHandler = retryHandler;

    public async Task FetchRepositoriesAsync(CancellationToken token)
    {
        _logger.LogInformation("Fetching repository metadata from {RepositoriesUrl}.", repositoriesUrl);

        using var client = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(_settings.Timeout)
        };

        try
        {
            var repo = await client.GetFromJsonAsync<Dictionary<string, Uri>>(repositoriesUrl, _options, token);
            if (repo is null)
            {
                _logger.LogWarning("Repository metadata request returned no data.");
                return;
            }

            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = _settings.MaxConcurrency,
                CancellationToken = token
            };
            var lists = new ConcurrentBag<ModListMetadata>();

            await Parallel.ForEachAsync(repo, options, (kvp, ct) => new ValueTask(_retryHandler.AutoRetryAsync(() => Fetch(kvp, ct), null, ct)));
            Repositories = lists.OrderBy(t => t.Title).ToArray();
            _logger.LogInformation("Loaded {ModListCount} mod lists from {RepositoryCount} repositories.", Repositories.Length, repo.Count);

            async Task Fetch(KeyValuePair<string, Uri> item, CancellationToken token)
            {
                try
                {
                    var metadata = await client.GetFromJsonAsync<ModListMetadata[]>(item.Value, _options, token);
                    if (metadata is not null)
                    {
                        foreach (var entry in metadata)
                        {
                            lists.Add(entry);
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    // Attempt to fetch the metadata again if we hit TooManyRequests or a transient error
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch ModListMetadata from {Repository} at {Url}. Skipping.", item.Key, item.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch repository metadata from {RepositoriesUrl}.", repositoriesUrl);
            throw;
        }
    }
}
