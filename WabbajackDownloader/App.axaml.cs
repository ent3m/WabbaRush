using Avalonia;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using WabbajackDownloader.Common.Configuration;
using WabbajackDownloader.Common.Logging;
using WabbajackDownloader.Common.Platform;
using WabbajackDownloader.Common.Retry;
using WabbajackDownloader.Common.Serialization;
using WabbajackDownloader.Features.Dashboard;
using WabbajackDownloader.Features.WabbajackModList.Services;
using WabbajackDownloader.Features.WabbajackRepo;
using WabbajackDownloader.Features.WebView;
using Waypoint;

namespace WabbajackDownloader;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collections = new ServiceCollection();
        var services = collections
            .AddSerialization()
            .AddConfiguration()
            .AddLogger()
            .AddRetry()
            .AddPlatform()
            .AddNavigation(ApplicationLifetime, RegisterFeatures)
            .AddWabbajackRepo()
            .AddWabbajackModList()
            .AddDashboard()
            .AddNexusMods()
            .BuildServiceProvider();

        // Start the application with the splash screen
        var navigation = services.GetRequiredService<INavigator>();
        navigation.NavigateWindowAsync<Splashscreen>();

        base.OnFrameworkInitializationCompleted();
    }

    private static void RegisterFeatures(IViewRegistry views) => views
        .RegisterDashboard();
}