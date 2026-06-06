using WabbajackDownloader.Common.Update;
using WabbajackDownloader.Features.Dashboard;
using Waypoint;

namespace WabbajackDownloader.Features.Frame;

internal sealed partial class MainWindowViewModel(INavigator navigator, UpdateHandler updateHandler) : INavigable
{
    private readonly INavigator _navigator = navigator;

    async Task INavigable.OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken)
    {
        await _navigator.NavigateAsync<SetupPage, Shell>(cancellationToken: cancellationToken);

        // Show update notification if app is outdated
        if (parameter is bool isOutdated && isOutdated)
        {
            // Dismiss popup after 5 seconds
            using var cts = new CancellationTokenSource(5000);
            await updateHandler.CheckForUpdateAndShowNotificationAsync(cts.Token);
        }
    }
}
