using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Windows.Input;
using WabbajackDownloader.Features.WebView.Interop;

namespace WabbajackDownloader.Features.WebView;

/// <summary>
/// A control that extends NativeWebView to provide additional functionalities specific to WebView2.
/// </summary>
internal class WebView2 : NativeWebView
{
    #region Avalonia Properties
    public static readonly StyledProperty<bool> AllowPopupProperty =
        AvaloniaProperty.Register<WebView2, bool>(nameof(AllowPopup), defaultValue: true);
    public bool AllowPopup
    {
        get => GetValue(AllowPopupProperty);
        set => SetValue(AllowPopupProperty, value);
    }
    private static void PreventNewWindow(object? sender, WebViewNewWindowRequestedEventArgs e) => e.Handled = true;

    public static readonly StyledProperty<string?> DefaultDownloadFolderPathProperty =
        AvaloniaProperty.Register<WebView2, string?>(nameof(DefaultDownloadFolderPath));
    /// <summary>
    /// The default download folder path.
    /// </summary>
    /// <remarks>
    /// The default value is the system default download folder path for the user.
    /// The default download folder path is persisted in the user data folder across sessions.
    /// The value should be an absolute path to a folder that the user and application can write to.
    /// Throws an exception if the value is invalid, and the default download path is not changed.
    /// Otherwise the path is changed immediately. If the directory does not yet exist, it is created at the time of the next download.
    /// If the host application does not have permission to create the directory, then the user is prompted to provide a new path through the Save As dialog.
    /// The user can override the default download folder path for a given download by choosing a different path in the Save As dialog.
    /// </remarks>
    public string? DefaultDownloadFolderPath
    {
        get => GetValue(DefaultDownloadFolderPathProperty);
        set => SetValue(DefaultDownloadFolderPathProperty, value);
    }

    // Parameter type: Uri
    public static readonly StyledProperty<ICommand?> NavigationCompletedCommandProperty =
        AvaloniaProperty.Register<WebView2, ICommand?>(nameof(NavigationCompletedCommand));
    public ICommand? NavigationCompletedCommand
    {
        get => GetValue(NavigationCompletedCommandProperty);
        set => SetValue(NavigationCompletedCommandProperty, value);
    }
    private void OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        // Only execute if navigation is successful
        if (!e.IsSuccess) return;

        var command = NavigationCompletedCommand;
        if (command?.CanExecute(e.Request) == true)
            command.Execute(e.Request);
    }

    // Parameter type: DownloadStartingEventArgs
    public static readonly StyledProperty<ICommand?> DownloadStartingCommandProperty =
        AvaloniaProperty.Register<WebView2, ICommand?>(nameof(DownloadStartingCommand));
    public ICommand? DownloadStartingCommand
    {
        get => GetValue(DownloadStartingCommandProperty);
        set => SetValue(DownloadStartingCommandProperty, value);
    }
    /// <summary>
    /// Raised when a download has begun, blocking the default download dialog, but not blocking the progress of the download.
    /// </summary>
    public event EventHandler<DownloadStartingEventArgs>? DownloadStarting;
    private void OnDownloadStarting(ICoreWebView2DownloadStartingEventArgs? args)
    {
        // Only execute if we get a valid download starting event from CoreWebView2
        if (args is null) return;

        var eventArgs = new DownloadStartingEventArgs(args);
        DownloadStarting?.Invoke(this, eventArgs);

        var command = DownloadStartingCommand;
        if (command?.CanExecute(eventArgs) == true)
            command.Execute(eventArgs);
    }

    public static readonly StyledProperty<WebViewDefaultDownloadDialogCornerAlignment> DefaultDownloadDialogCornerAlignmentProperty =
        AvaloniaProperty.Register<WebView2, WebViewDefaultDownloadDialogCornerAlignment>(nameof(DefaultDownloadDialogCornerAlignment),
            defaultValue: WebViewDefaultDownloadDialogCornerAlignment.TopRight);
    public WebViewDefaultDownloadDialogCornerAlignment DefaultDownloadDialogCornerAlignment
    {
        get => GetValue(DefaultDownloadDialogCornerAlignmentProperty);
        set => SetValue(DefaultDownloadDialogCornerAlignmentProperty, value);
    }

    public static readonly StyledProperty<IJavaScriptExecutionEngine?> JavaScriptExecutionEngineProperty =
        AvaloniaProperty.Register<WebView2, IJavaScriptExecutionEngine?>(nameof(JavaScriptExecutionEngine));
    public IJavaScriptExecutionEngine? JavaScriptExecutionEngine
    {
        get => GetValue(JavaScriptExecutionEngineProperty);
        set => SetValue(JavaScriptExecutionEngineProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == AllowPopupProperty)
        {
            // Unsubscribe first to prevent duplicate handlers
            base.NewWindowRequested -= PreventNewWindow;

            // Attach blocking handler if popups are not allowed
            if (change.GetNewValue<bool>() == false)
                base.NewWindowRequested += PreventNewWindow;
        }
        else if (change.Property == DefaultDownloadFolderPathProperty)
        {
            // Make sure path is propagated to CoreWebView2 profile
            var newValue = change.GetNewValue<string?>() ?? string.Empty;
            _coreWebView.GetProfile(out var profile);
            profile.PutDefaultDownloadFolderPath(newValue);
        }
        else if (change.Property == DefaultDownloadDialogCornerAlignmentProperty)
        {
            // Make sure alignment is propagated to CoreWebView2
            var newValue = change.GetNewValue<WebViewDefaultDownloadDialogCornerAlignment>();
            _coreWebView.PutDefaultDownloadDialogCornerAlignment(newValue);
        }
        else if (change.Property == JavaScriptExecutionEngineProperty)
        {
            // Detach old engine to prevent memory leak
            var oldValue = change.GetOldValue<IJavaScriptExecutionEngine?>();
            oldValue?.Detach();
            // Attach new engine
            var newValue = change.GetNewValue<IJavaScriptExecutionEngine?>();
            newValue?.Attach(this);
        }
    }
    #endregion

    private static readonly StrategyBasedComWrappers ComWrappers = new();
    private EventRegistrationToken _downloadStartingToken;
#pragma warning disable CS8618 // _coreWebView assigned in OnAdapterCreated, which is guaranteed to be called before the WebView2 is used.
    private ICoreWebView2_13 _coreWebView;

    public WebView2()
    {
        this.AdapterCreated += OnAdapterCreated;
        this.AdapterDestroyed += OnAdapterDestroyed;
    }
#pragma warning restore CS8618
    private void OnAdapterCreated(object? sender, WebViewAdapterEventArgs args) => Initialize();
    private void OnAdapterDestroyed(object? sender, WebViewAdapterEventArgs args) => Cleanup();
    // Cleanup on unloaded as well, because OnAdapterDestroyed is not reliably invoked
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        Cleanup();
    }

    private void Initialize()
    {
        _coreWebView = GetCoreWebView();

        // Kickstart the DefaultDownloadFolderPath property with the current value from the profile
        _coreWebView.GetProfile(out var profile);
        profile.GetDefaultDownloadFolderPath(out var path);
        SetValue(DefaultDownloadFolderPathProperty, path);

        // Hook up the download starting event to raise our own DownloadStarting event and command
        var downloadStartingHandler = new CoreWebView2DownloadStartingHandler(OnDownloadStarting);
        _coreWebView.AddDownloadStarting(downloadStartingHandler, out _downloadStartingToken);

        // Hook up the navigation completed event to raise our own NavigationCompleted command
        base.NavigationCompleted -= OnNavigationCompleted;  // Make sure we never subscribe twice
        base.NavigationCompleted += OnNavigationCompleted;
    }
    private ICoreWebView2_13 GetCoreWebView()
    {
        // Get the underlying CoreWebView2
        if (TryGetPlatformHandle() is not IWindowsWebView2PlatformHandle handle)
            throw new InvalidOperationException("Not running on Windows WebView2." +
                "Make sure you have the WebView2 runtime installed: https://developer.microsoft.com/microsoft-edge/webview2/consumer/");

        // Get the managed interface from the COM object using StrategyBasedComWrappers
        var rawWrapper = ComWrappers.GetOrCreateObjectForComInstance(handle.CoreWebView2, CreateObjectFlags.None);
        return rawWrapper as ICoreWebView2_13
            ?? throw new InvalidOperationException("CoreWebView2 does not implement ICoreWebView2_13. Ensure the WebView2 runtime is up to date.");
    }
    private void Cleanup()
    {
        // Remove all references to this object from CoreWebView2
        _coreWebView?.RemoveDownloadStarting(_downloadStartingToken);
        // Detach JavaScript execution engine to prevent memory leak
        JavaScriptExecutionEngine?.Detach();
    }
}