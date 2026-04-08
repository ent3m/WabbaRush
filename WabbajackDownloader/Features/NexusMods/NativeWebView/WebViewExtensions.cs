using Avalonia;
using Avalonia.Controls;
using System.Windows.Input;

namespace WabbajackDownloader.Features.NexusMods;

public class WebViewExtensions : AvaloniaObject
{
    static WebViewExtensions()
    {
        NavigationCompletedCommandProperty.Changed.AddClassHandler<NativeWebView>(OnNavigationCompletedCommandChanged);
        AllowPopupsProperty.Changed.AddClassHandler<NativeWebView>(OnAllowPopupsChanged);
        ControllerProperty.Changed.AddClassHandler<NativeWebView>(OnControllerChanged);
    }

    #region NavigationCompleted
    public static readonly AttachedProperty<ICommand?> NavigationCompletedCommandProperty =
        AvaloniaProperty.RegisterAttached<NativeWebView, ICommand?>(
            "NavigationCompletedCommand", typeof(WebViewExtensions));
    public static void SetNavigationCompletedCommand(AvaloniaObject element, ICommand? value)
        => element.SetValue(NavigationCompletedCommandProperty, value);
    public static ICommand? GetNavigationCompletedCommand(AvaloniaObject element)
        => element.GetValue(NavigationCompletedCommandProperty);

    private static void OnNavigationCompletedCommandChanged(NativeWebView webView, AvaloniaPropertyChangedEventArgs e)
    {
        // Unsubscribe from the old command
        if (e.OldValue is ICommand)
            webView.NavigationCompleted -= NavigationCompleted;

        // Subscribe to the new command
        if (e.NewValue is ICommand)
            webView.NavigationCompleted += NavigationCompleted;
    }

    private static void NavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        if (sender is NativeWebView webView)
        {
            // Only execute the command if navigation was successful
            if (!e.IsSuccess)
                return;

            var command = GetNavigationCompletedCommand(webView);
            if (command?.CanExecute(e.Request) == true)
                command.Execute(e.Request);
        }
    }
    #endregion

    #region AllowPopup
    public static readonly AttachedProperty<bool> AllowPopupsProperty =
        AvaloniaProperty.RegisterAttached<NativeWebView, bool>(
            "AllowPopups", typeof(WebViewExtensions), true);
    public static void SetAllowPopups(NativeWebView element, bool value)
        => element.SetValue(AllowPopupsProperty, value);
    public static bool GetAllowPopups(NativeWebView element)
        => element.GetValue(AllowPopupsProperty);

    private static void OnAllowPopupsChanged(NativeWebView webView, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not bool isAllowed)
            return;

        // Unsubscribe first to prevent duplicate handlers
        webView.NewWindowRequested -= PreventNewWindow;

        // Attach blocking handler if popups are not allowed
        if (!isAllowed)
            webView.NewWindowRequested += PreventNewWindow;
    }

    private static void PreventNewWindow(object? sender, WebViewNewWindowRequestedEventArgs e)
        => e.Handled = true;
    #endregion

    #region Controller
    public static readonly AttachedProperty<WebViewController?> ControllerProperty =
        AvaloniaProperty.RegisterAttached<NativeWebView, WebViewController?>(
            "Controller", typeof(WebViewExtensions));
    public static void SetController(AvaloniaObject element, WebViewController? value)
        => element.SetValue(ControllerProperty, value);
    public static WebViewController? GetController(AvaloniaObject element)
        => element.GetValue(ControllerProperty);

    private static void OnControllerChanged(NativeWebView webView, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is WebViewController controller)
            controller.Attach(script => webView.InvokeScript(script));
    }
    #endregion
}