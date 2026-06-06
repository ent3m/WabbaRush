using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace WabbajackDownloader.Features.Dashboard;

/// <summary>
/// A wrapper designed to block all keyboard and mouse events from reaching its content when the <see cref="IsLocked"/> property is set to true.
/// This is achieved by using a transparent popup that covers the entire area of the control.
/// Popup specifically set ShouldUseOverlayLayer="False" to work around NativeHostContainer.
/// </summary>
internal sealed class InputBlocker : ContentControl
{
    private Popup? _popup;
    private Border? _shield;
    private Window? _window;
    private bool _windowIsActive = true;

    public static readonly StyledProperty<bool> IsLockedProperty =
        AvaloniaProperty.Register<InputBlocker, bool>(nameof(IsLocked));

    public bool IsLocked
    {
        get => GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    public InputBlocker()
    {
        AddHandler(PointerPressedEvent, OnInput, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnInput, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnInput, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerWheelChangedEvent, OnInput, RoutingStrategies.Tunnel, handledEventsToo: true);

        AddHandler(KeyDownEvent, OnInput, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(KeyUpEvent, OnInput, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(TextInputEvent, OnInput, RoutingStrategies.Tunnel, handledEventsToo: true);

        AddHandler(DragDrop.DragEnterEvent, OnInput, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(DragDrop.DragOverEvent, OnInput, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(DragDrop.DropEvent, OnInput, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var window = TopLevel.GetTopLevel(this) as Window;
        if (window is not null)
        {
            window.Activated += OnWindowActivated;
            window.Deactivated += OnWindowDeactivated;
        }
        _window = window;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_window is not null)
        {
            _window.Activated -= OnWindowActivated;
            _window.Deactivated -= OnWindowDeactivated;
        }
        _window = null;

        base.OnDetachedFromVisualTree(e);
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        _windowIsActive = true;
        ApplyLockedState();
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        _windowIsActive = false;
        ApplyLockedState();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _popup = e.NameScope.Find<Popup>("PART_Popup");
        _shield = e.NameScope.Find<Border>("PART_Shield");

        UpdateShieldSize(Bounds.Size);
        ApplyLockedState();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Size arranged = base.ArrangeOverride(finalSize);
        UpdateShieldSize(arranged);
        return arranged;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsLockedProperty)
        {
            ApplyLockedState();
        }
    }

    /// <summary>
    /// Enable/disable the popup based on the <see cref="IsLocked"/> property and whether the host window is active.
    /// </summary>
    private void ApplyLockedState()
        => _popup?.IsOpen = IsLocked && _windowIsActive;

    /// <summary>
    /// Match popup shield size to InputBlocker size.
    /// </summary>
    private void UpdateShieldSize(Size size)
    {
        if (_shield is null)
            return;

        _shield.Width = size.Width;
        _shield.Height = size.Height;
    }

    /// <summary>
    /// Mark all input events as handled.
    /// Events that originate from the shield will have InputBlocker as sender as well, since the shield part of InputBlocker's ControlTemplate.
    /// </summary>
    private static void OnInput(object? sender, RoutedEventArgs e)
    {
        if (sender is InputBlocker { IsLocked: true })
        {
            e.Handled = true;
        }
    }
}
