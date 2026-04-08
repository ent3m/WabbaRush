using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using WabbajackDownloader.Features.WabbajackRepo;
using Waypoint;

namespace WabbajackDownloader.Features.Dashboard;

internal partial class SplashscreenViewModel : ObservableObject, INavigable
{
    [ObservableProperty]
    public partial string LoadingText { get; set; } = "Loading";

    private readonly RepositoriesDownloader _repoDownloader;
    private readonly INavigator _navigator;
    private readonly DispatcherTimer _timer;
    private int _dotCount = 0;

    public SplashscreenViewModel(RepositoriesDownloader repoDownloader, INavigator navigator)
    {
        _repoDownloader = repoDownloader;
        _navigator = navigator;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(0.5),
        };
        _timer.Tick += UpdateDots;
        _timer.Start();
    }

    // Cycles loading text with 0, 1, 2, 3 dots
    private void UpdateDots(object? s, EventArgs e)
    {
        _dotCount = ((_dotCount + 1) % 4);
        LoadingText = "Fetching Repositories" + new string('.', _dotCount);
    }

    async Task INavigable.OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken)
    {
        await _repoDownloader.FetchRepositoriesAsync(cancellationToken);
        await _navigator.NavigateWindowAsync<MainWindow>(null, cancellationToken);
    }
}
