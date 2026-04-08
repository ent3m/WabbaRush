using Avalonia.Controls;
using Avalonia.Threading;
using System.Net;
using WabbajackDownloader.Features.NexusMods;

namespace WabbajackDownloader.Views;

public partial class NexusSigninWindow : Window
{
    private const string loginPage = "https://users.nexusmods.com/auth/sign_in?redirect_url=";
    private readonly AutoDownloadCefBrowser browser;
    private readonly TaskCompletionSource<CookieContainer> tcs = new();
    private readonly ILogger? logger;

#if DEBUG
    // parameterless constructor for xaml previewer
    public NexusSigninWindow() : this(string.Empty, null)
    {

    }
#endif

    public NexusSigninWindow(string nexusLandingPage, ILogger? logger)
    {
        InitializeComponent();

        this.logger = logger;
        var address = loginPage + Uri.EscapeDataString(nexusLandingPage);
        logger?.LogDebug("Creating cef browser...");
        browser = new AutoDownloadCefBrowser(this, RequestContextFactory, logger)
        {
            Address = address,
            LifeSpanHandler = new PopupLifeSpanHandler(this),
        };
        browser.LoadStart += OnBrowserLoadStart;
        browserContainer.Child = browser;
        logger?.LogDebug("Cef browser created and initialized.");
        static CefRequestContext RequestContextFactory()
        {
            CefRequestContextSettings settings = new()
            {
                PersistSessionCookies = true,
            };
            CefRequestContext context = CefRequestContext.CreateContext(settings, null);
            return context;
        }
    }

    internal AutoDownloadCefBrowser ShowAndGetBrowser(Window owner)
    {
        Show(owner);
        return browser;
    }

    /// <summary>
    /// Update address text when page begins loading if we're not in a popup browser
    /// </summary>
    private void OnBrowserLoadStart(object sender, LoadStartEventArgs e)
    {
        if (e.Frame.Browser.IsPopup || !e.Frame.IsMain)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            addressText.Text = e.Frame.Url;
        });
    }

    /// <summary>
    /// Prevent this window from being closed by the user
    /// </summary>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (e.CloseReason == WindowCloseReason.WindowClosing && !e.IsProgrammatic)
        {
            e.Cancel = true;
            this.Hide();
        }
        else
            base.OnClosing(e);
    }
}
