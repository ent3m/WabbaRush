using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using WabbajackDownloader.Core;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Handlers;

namespace WabbajackDownloader.Views;

public partial class NexusSigninWindow : Window, IDisposable
{
    private const string loginPage = "https://users.nexusmods.com/auth/sign_in?redirect_url=";
    private readonly AvaloniaCefBrowser browser;
    private readonly TaskCompletionSource<CookieContainer> tcs = new();

    public NexusSigninWindow()
    {
        InitializeComponent();

        var address = loginPage + Uri.EscapeDataString(App.Settings.NexusLandingPage);
        browser = new AvaloniaCefBrowser(RequestContextFactory)
        {
            Address = address,
            LifeSpanHandler = new PopupLifeSpanHandler(),
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
        var manager = browser.RequestContext.GetCookieManager(null);
        var visitor = new NexusCookieVisitor(manager, tcs);
        visitor.GetCookies();
    }

    public void Dispose()
    {
        browser.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Launch popup in a separate window that shares its lifetime with the main window
    /// </summary>
    private class PopupLifeSpanHandler : LifeSpanHandler
    {
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
                    LifeSpanHandler = new PopupLifeSpanHandler(),
                    DownloadHandler = new DownloadNothingHandler(),
                };
                window.Content = popupBrowser;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Height = 270;
                window.Width = 480;
                window.Title = targetUrl;
                window.Show(App.SigninWindow ?? App.MainWindow);
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
    private class NexusCookieVisitor(CefCookieManager manager, TaskCompletionSource<CookieContainer> tcs) : CefCookieVisitor
    {
        private readonly CefCookieManager manager = manager;
        private readonly CookieContainer container = new();
        private readonly TaskCompletionSource<CookieContainer> tcs = tcs;

        public void GetCookies()
        {
            if (!manager.VisitAllCookies(this))
                throw new AccessViolationException("Unable to access cookies.");
        }

        protected override bool Visit(CefCookie cookie, int count, int total, out bool deleteCookie)
        {
            var convertedCookie = CookieHelper.ConvertCefCookie(cookie);
            container.Add(convertedCookie);

            App.Logger.LogInformation("Added cookie No. {count}/{total}:\n{cookie}", count + 1, total, CookieHelper.ToString(convertedCookie));

            deleteCookie = false;
            // cookies finish enumerating when count == total -1 or when returning false
            if (count == total - 1)
            {
                tcs.SetResult(container);
                return false;
            }
            return true;
        }
    }
}