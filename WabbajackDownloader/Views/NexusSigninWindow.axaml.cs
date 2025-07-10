using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;
using WabbajackDownloader.Extensions;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Handlers;

namespace WabbajackDownloader.Views;

public partial class NexusSigninWindow : Window
{
    private const string loginPage = "https://users.nexusmods.com/auth/sign_in?redirect_url=";
    private readonly AvaloniaCefBrowser browser;
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
        browser = new AvaloniaCefBrowser(RequestContextFactory)
        {
            Address = address,
            LifeSpanHandler = new PopupLifeSpanHandler(this),
            DownloadHandler = new DownloadNothingHandler(),
        };
        browser.LoadStart += OnBrowserLoadStart;
        browserContainer.Child = browser;

        static CefRequestContext RequestContextFactory()
        {
            CefRequestContextSettings settings = new()
            {
                PersistSessionCookies = false,
            };
            CefRequestContext context = CefRequestContext.CreateContext(settings, null);
            return context;
        }
    }

    public async Task<CookieContainer> ShowAndGetCookiesAsync(Window owner)
    {
#pragma warning disable CS4014 // we don't wait for dialog window to close here
        ShowDialog(owner);
#pragma warning restore CS4014 // instead, we're waiting for cookies to be fetched
        var result = await tcs.Task;
        return result;
    }

    /// <summary>
    /// Fetch cookies when this browser is closing
    /// </summary>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!tcs.Task.IsCompleted)
            GetCookies();
        base.OnClosing(e);
    }

    /// <summary>
    /// Update address text when page begins loading if we're not in a popup browser
    /// </summary>
    private void OnBrowserLoadStart(object sender, Xilium.CefGlue.Common.Events.LoadStartEventArgs e)
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
    /// Begin fetching cookies so that the task can be completed
    /// </summary>
    private void GetCookies()
    {
        try
        {
            var manager = browser.RequestContext.GetCookieManager(null);
            var visitor = new NexusCookieVisitor(manager, tcs, logger);
            visitor.GetCookies();
        }
        catch (Exception ex)
        {
            logger?.LogCritical(ex, "Failed to get cookies from nexusmods.");
        }
    }

    /// <summary>
    /// Launch popup in a separate window that shares its lifetime with the main window
    /// </summary>
    private class PopupLifeSpanHandler(Window owner) : LifeSpanHandler
    {
        private readonly Window owner = owner;

        protected override bool OnBeforePopup(CefBrowser browser, CefFrame frame, string targetUrl, string targetFrameName, CefWindowOpenDisposition targetDisposition,
            bool userGesture, CefPopupFeatures popupFeatures, CefWindowInfo windowInfo, ref CefClient client, CefBrowserSettings settings,
            ref CefDictionaryValue extraInfo, ref bool noJavascriptAccess)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var window = new Window();
                var popupBrowser = new AvaloniaCefBrowser
                {
                    Address = targetUrl,
                    LifeSpanHandler = new PopupLifeSpanHandler(window),
                    DownloadHandler = new DownloadNothingHandler(),
                };
                window.Content = popupBrowser;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Height = 270;
                window.Width = 480;
                window.Title = targetUrl;
                window.Show(owner);
            });
            return true;
        }
    }

    /// <summary>
    /// Make sure nothing is downloaded
    /// </summary>
    private class DownloadNothingHandler : DownloadHandler
    {
        protected override bool CanDownload(CefBrowser browser, string url, string requestMethod)
            => false;
    }

    /// <summary>
    /// Retrieve all cookies from nexusmods.com and signal the completion via the provided TaskCompletionSource
    /// </summary>
    private class NexusCookieVisitor(CefCookieManager manager, TaskCompletionSource<CookieContainer> tcs, ILogger? logger) : CefCookieVisitor
    {
        private readonly CefCookieManager manager = manager;
        private readonly ILogger? logger = logger;
        private readonly CookieContainer container = new();
        private readonly TaskCompletionSource<CookieContainer> tcs = tcs;

        public void GetCookies()
        {
            if (!manager.VisitAllCookies(this))
            {
                var exception = new AccessViolationException("Unable to access cookies.");
                logger?.LogCritical(exception, "Cannot access cookies of {manager}.", manager);
                throw exception;
            }
        }

        protected override bool Visit(CefCookie cookie, int count, int total, out bool deleteCookie)
        {
            var convertedCookie = cookie.ConvertCookie();
            container.Add(convertedCookie);

            logger?.LogTrace("Added cookie No. {count}/{total}:\n{cookie}", count + 1, total, convertedCookie.ToString());

            deleteCookie = false;
            // cookies finish enumerating when count == total -1 or when returning false
            if (count == total - 1)
            {
                logger?.LogInformation("Added {count} cookies to container.", container.Count);
                logger?.LogTrace("Cookie header: {header}", container.GetCookieHeader(new Uri("https://www.nexusmods.com/Core/Libs/Common/Managers/Downloads?GenerateDownloadUrl")));
                tcs.SetResult(container);
                return false;
            }
            return true;
        }
    }
}