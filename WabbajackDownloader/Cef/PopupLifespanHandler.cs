using Avalonia.Controls;
using Avalonia.Threading;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Handlers;

namespace WabbajackDownloader.Cef;

/// <summary>
/// Launch popup in a separate window that shares its lifetime with the owner window
/// </summary>
internal class PopupLifeSpanHandler(Window owner) : LifeSpanHandler
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
