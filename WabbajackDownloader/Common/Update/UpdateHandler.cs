using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using WabbajackDownloader.Common.Dialogs;
using WabbajackDownloader.Common.Serialization;
using Waypoint;

namespace WabbajackDownloader.Common.Update;

// GitHub has API rate limits, so we should register this handler as singleton and only check once per session.
internal sealed class UpdateHandler(INavigator navigator, ILogger<UpdateHandler> logger)
{
    public Version? CurrentVersion { get; private set; }
    public Version? LatestVersion { get; private set; }
    public bool IsOutdated => LatestVersion > CurrentVersion;
    public bool UpdateChecked { get; private set; } = false;

    public async Task CheckForUpdateAndShowNotificationAsync(CancellationToken cancellationToken)
    {
        if (!UpdateChecked)
            await CheckForUpdateAsync(cancellationToken);

        string message;
        if (IsOutdated)
            message = $"""
                A new version is available!
                Current: {CurrentVersion?.ToString(3)}
                Latest: {LatestVersion}
                """;
        else
            message = "WabbaRush is up to date! ☺";

        var options = new ToastOptions(message, ToastType.Neutral, 16);
        await navigator.ShowPopupAsync<Toast>(parameter: options, verticalPlacement: Avalonia.Layout.VerticalAlignment.Top, cancellationToken: cancellationToken);
    }

    public async Task CheckForUpdateAsync(CancellationToken cancellationToken)
    {
        if (UpdateChecked) return;

        try
        {
            using var client = new HttpClient();
            // GitHub API requires a User-Agent header
            client.DefaultRequestHeaders.Add("User-Agent", "WabbaRush");

            var response = await client.GetFromJsonAsync<GitHubRelease>("https://api.github.com/repos/ent3m/WabbaRush/releases/latest",
                SourceGenerationContext.Default.GitHubRelease, cancellationToken);

            if (response != null && Version.TryParse(response.TagName.TrimStart('v'), out var latestVersion))
            {
                LatestVersion = latestVersion;
                CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                logger.LogInformation("Current version: {CurrentVersion}; Latest version: {LatestVersion}", CurrentVersion, LatestVersion);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch the latest version of the app.");
        }

        UpdateChecked = true;
    }
}
