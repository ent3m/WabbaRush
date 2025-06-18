using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Handlers;

namespace WabbajackDownloader.Views;

public partial class NexusSigninWindow : Window
{
    private readonly AvaloniaCefBrowser browser;
    private const string loginPage = "https://users.nexusmods.com/auth/sign_in?redirect_url=https%3A%2F%2Fwww.nexusmods.com%2Fskyrimspecialedition%2Fmods%2F12604";
    private readonly TaskCompletionSource<CookieContainer> tcs = new();

    public NexusSigninWindow()
    {
        InitializeComponent();

        browser = new AvaloniaCefBrowser(RequestContextFactory)
        {
            Address = loginPage,
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
#pragma warning disable CS4014 // This call is not meant to be awaited. It's used to show this window as a dialog window.
        ShowDialog(owner);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        var result = await tcs.Task;
        browser.Dispose();
        return result;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!tcs.Task.IsCompleted)
            GetCookies();
        base.OnClosing(e);
    }

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

    private void GetCookies()
    {
        var manager = browser.RequestContext.GetCookieManager(null);
        var visitor = new NexusCookieVisitor(manager, tcs);
        visitor.GetCookies();
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
    private class NexusCookieVisitor : CefCookieVisitor
    {
        private readonly CefCookieManager manager;
        private readonly CookieContainer container = new();
        private readonly TaskCompletionSource<CookieContainer> tcs;

        public NexusCookieVisitor(CefCookieManager manager, TaskCompletionSource<CookieContainer> tcs)
        {
            this.manager = manager;
            this.tcs = tcs;
        }

        public void GetCookies()
        {
            if (!manager.VisitAllCookies(this))
                throw new Exception("Unable to access cookies.");
        }

        protected override bool Visit(CefCookie cookie, int count, int total, out bool deleteCookie)
        {
            var convertedCookie = CookieHelper.ConvertCefCookie(cookie);
            container.Add(convertedCookie);

#if DEBUG
            Debug.WriteLine($"Printing Cookie No. {count + 1}/{total}...");
            Debug.WriteLine(CookieHelper.ToString(convertedCookie));
#endif

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