using Avalonia;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using WabbajackDownloader.Common.Dialogs;
using WabbajackDownloader.Common.Logging;
using WabbajackDownloader.Common.Platform;
using WabbajackDownloader.Common.Retry;
using WabbajackDownloader.Common.Themes;
using WabbajackDownloader.Common.Update;
using WabbajackDownloader.Features.Dashboard;
using WabbajackDownloader.Features.Frame;
using WabbajackDownloader.Features.Splashscreen;
using WabbajackDownloader.Features.WabbajackModList.Services;
using WabbajackDownloader.Features.WabbajackRepo;
using WabbajackDownloader.Features.WebView;
using Waypoint;

namespace WabbajackDownloader;

internal partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collections = new ServiceCollection();
        var services = collections
            .AddConfiguration()
            .AddLogger()
            .AddRetry()
            .AddPlatform()
            .AddUpdate()
            .AddNavigation(ApplicationLifetime, RegisterFeatures)
            .AddWabbajackRepo()
            .AddWabbajackModList()
            .AddSplashscreen()
            .AddFrame()
            .AddDashboard()
            .AddWebView()
            .AddDialog()
            .AddThemes()
            .BuildServiceProvider();

        ThemeManager.Instance = services.GetRequiredService<ThemeManager>();

        base.OnFrameworkInitializationCompleted();

        // Start the application with the splashscreen
        var navigation = services.GetRequiredService<INavigator>();
        navigation.NavigateWindowAsync<Splashscreen>();
    }

    private static void RegisterFeatures(IViewRegistry views) => views
        .RegisterSplashscreen()
        .RegisterFrame()
        .RegisterDashboard()
        .RegisterDialogs();
}