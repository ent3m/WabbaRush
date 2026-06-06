using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using System.Globalization;

namespace WabbajackDownloader.Common.Progress;

[PseudoClasses(":has-content", ":indeterminate")]
internal sealed class CircularProgressBar : RangeBase
{
    private const double DefaultSize = 64.0;
    private const double FullCircleThreshold = 0.999_999;
    private const double IndeterminateProgressSize = 0.25;

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<CircularProgressBar, double>(
            nameof(StrokeThickness),
            defaultValue: 7.0);

    public static readonly StyledProperty<IBrush?> TrackBrushProperty =
        AvaloniaProperty.Register<CircularProgressBar, IBrush?>(
            nameof(TrackBrush));

    public static readonly StyledProperty<IBrush?> ProgressBrushProperty =
        AvaloniaProperty.Register<CircularProgressBar, IBrush?>(
            nameof(ProgressBrush));

    public static readonly StyledProperty<double> StartAngleProperty =
        AvaloniaProperty.Register<CircularProgressBar, double>(
            nameof(StartAngle),
            defaultValue: -90.0);

    public static readonly StyledProperty<PenLineCap> StrokeLineCapProperty =
        AvaloniaProperty.Register<CircularProgressBar, PenLineCap>(
            nameof(StrokeLineCap),
            defaultValue: PenLineCap.Round);

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<CircularProgressBar, object?>(
            nameof(Content));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<CircularProgressBar, IDataTemplate?>(
            nameof(ContentTemplate));

    public static readonly StyledProperty<string?> ProgressTextFormatProperty =
        AvaloniaProperty.Register<CircularProgressBar, string?>(
            nameof(ProgressTextFormat),
            defaultValue: "{1:0}%");

    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        AvaloniaProperty.Register<CircularProgressBar, HorizontalAlignment>(
            nameof(HorizontalContentAlignment),
            defaultValue: HorizontalAlignment.Center);

    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        AvaloniaProperty.Register<CircularProgressBar, VerticalAlignment>(
            nameof(VerticalContentAlignment),
            defaultValue: VerticalAlignment.Center);

    public static readonly DirectProperty<CircularProgressBar, string> ProgressTextProperty =
        AvaloniaProperty.RegisterDirect<CircularProgressBar, string>(
            nameof(ProgressText),
            static owner => owner.ProgressText);

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<CircularProgressBar, bool>(
            nameof(IsIndeterminate));

    public static readonly StyledProperty<double> IndeterminateAngleProperty =
        AvaloniaProperty.Register<CircularProgressBar, double>(
            nameof(IndeterminateAngle));

    private StreamGeometry? _cachedArcGeometry;
    private Point _cachedCenter;
    private double _cachedRadius;
    private double _cachedProgress;
    private double _cachedStartAngle;
    private string _progressText = string.Empty;

    static CircularProgressBar()
    {
        AffectsRender<CircularProgressBar>(
            MinimumProperty,
            MaximumProperty,
            ValueProperty,
            BackgroundProperty,
            ForegroundProperty,
            StrokeThicknessProperty,
            TrackBrushProperty,
            ProgressBrushProperty,
            StartAngleProperty,
            StrokeLineCapProperty,
            IsIndeterminateProperty,
            IndeterminateAngleProperty);

        AffectsMeasure<CircularProgressBar>(
            StrokeThicknessProperty);
    }

    public CircularProgressBar()
    {
        UpdateProgressText();
        UpdatePseudoClasses();
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public IBrush? ProgressBrush
    {
        get => GetValue(ProgressBrushProperty);
        set => SetValue(ProgressBrushProperty, value);
    }

    public double StartAngle
    {
        get => GetValue(StartAngleProperty);
        set => SetValue(StartAngleProperty, value);
    }

    public PenLineCap StrokeLineCap
    {
        get => GetValue(StrokeLineCapProperty);
        set => SetValue(StrokeLineCapProperty, value);
    }

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public IDataTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public string? ProgressTextFormat
    {
        get => GetValue(ProgressTextFormatProperty);
        set => SetValue(ProgressTextFormatProperty, value);
    }

    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    public string ProgressText
    {
        get => _progressText;
        private set => SetAndRaise(ProgressTextProperty, ref _progressText, value);
    }

    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    public double IndeterminateAngle
    {
        get => GetValue(IndeterminateAngleProperty);
        set => SetValue(IndeterminateAngleProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new CircularProgressBarAutomationPeer(this);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty
            || change.Property == MinimumProperty
            || change.Property == MaximumProperty
            || change.Property == ProgressTextFormatProperty)
        {
            UpdateProgressText();
        }

        if (change.Property == ContentProperty)
        {
            UpdatePseudoClasses();
        }

        if (change.Property == IsIndeterminateProperty)
        {
            UpdatePseudoClasses();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        bool hasWidth = double.IsFinite(availableSize.Width);
        bool hasHeight = double.IsFinite(availableSize.Height);

        double size = (hasWidth, hasHeight) switch
        {
            (true, true) => Math.Min(availableSize.Width, availableSize.Height),
            (true, false) => availableSize.Width,
            (false, true) => availableSize.Height,
            _ => DefaultSize,
        };

        return new Size(size, size);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        Size boundsSize = Bounds.Size;

        if (boundsSize.Width <= 0.0 || boundsSize.Height <= 0.0)
        {
            return;
        }

        double strokeThickness = StrokeThickness;

        if (!double.IsFinite(strokeThickness) || strokeThickness <= 0.0)
        {
            return;
        }

        double diameter = Math.Min(boundsSize.Width, boundsSize.Height);
        double radius = (diameter - strokeThickness) * 0.5;

        if (!double.IsFinite(radius) || radius <= 0.0)
        {
            return;
        }

        Point center = new(boundsSize.Width * 0.5, boundsSize.Height * 0.5);

        IBrush trackBrush = TrackBrush
            ?? Background
            ?? Brushes.LightGray;

        var trackPen = new Pen(trackBrush, strokeThickness)
        {
            LineCap = StrokeLineCap,
        };

        context.DrawEllipse(
            brush: null,
            pen: trackPen,
            center: center,
            radiusX: radius,
            radiusY: radius);

        double progress = IsIndeterminate ? IndeterminateProgressSize : GetNormalizedProgress();

        if (progress <= 0.0)
        {
            return;
        }

        double startAngle = IsIndeterminate ? StartAngle + IndeterminateAngle : StartAngle;

        IBrush progressBrush = ProgressBrush
            ?? Foreground
            ?? Brushes.DodgerBlue;

        var progressPen = new Pen(progressBrush, strokeThickness)
        {
            LineCap = StrokeLineCap,
        };

        if (progress >= FullCircleThreshold)
        {
            context.DrawEllipse(
                brush: null,
                pen: progressPen,
                center: center,
                radiusX: radius,
                radiusY: radius);

            return;
        }

        StreamGeometry arcGeometry = GetArcGeometry(
            center,
            radius,
            progress,
            startAngle);

        context.DrawGeometry(
            brush: null,
            pen: progressPen,
            geometry: arcGeometry);
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":has-content", Content is not null);
        PseudoClasses.Set(":indeterminate", IsIndeterminate);
    }

    private void UpdateProgressText()
    {
        double percentage = GetNormalizedProgress() * 100.0;
        string? format = ProgressTextFormat;

        ProgressText = string.IsNullOrWhiteSpace(format)
            ? string.Empty
            : string.Format(CultureInfo.CurrentCulture, format, Value, percentage, Minimum, Maximum);
    }

    private double GetNormalizedProgress()
    {
        double range = Maximum - Minimum;

        if (!double.IsFinite(range) || range <= 0.0)
        {
            return 0.0;
        }

        double progress = (Value - Minimum) / range;

        if (!double.IsFinite(progress))
        {
            return 0.0;
        }

        return Math.Clamp(progress, 0.0, 1.0);
    }

    private StreamGeometry GetArcGeometry(
        Point center,
        double radius,
        double progress,
        double startAngleDegrees)
    {
        if (_cachedArcGeometry is not null
            && _cachedCenter.Equals(center)
            && _cachedRadius.Equals(radius)
            && _cachedProgress.Equals(progress)
            && _cachedStartAngle.Equals(startAngleDegrees))
        {
            return _cachedArcGeometry;
        }

        double startAngle = DegreesToRadians(startAngleDegrees);
        double sweepAngle = Math.Tau * progress;

        Point startPoint = GetPointOnCircle(center, radius, startAngle);
        Point endPoint = GetPointOnCircle(center, radius, startAngle + sweepAngle);

        var geometry = new StreamGeometry();

        using (StreamGeometryContext stream = geometry.Open())
        {
            stream.BeginFigure(startPoint, isFilled: false);

            stream.ArcTo(
                point: endPoint,
                size: new Size(radius, radius),
                rotationAngle: 0.0,
                isLargeArc: sweepAngle > Math.PI,
                sweepDirection: SweepDirection.Clockwise,
                isStroked: true);

            stream.EndFigure(isClosed: false);
        }

        _cachedArcGeometry = geometry;
        _cachedCenter = center;
        _cachedRadius = radius;
        _cachedProgress = progress;
        _cachedStartAngle = startAngleDegrees;

        return geometry;
    }

    private static Point GetPointOnCircle(Point center, double radius, double angle)
    {
        return new Point(
            center.X + radius * Math.Cos(angle),
            center.Y + radius * Math.Sin(angle));
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}

file sealed class CircularProgressBarAutomationPeer(CircularProgressBar owner) : RangeBaseAutomationPeer(owner)
{
    protected override string GetClassNameCore()
    {
        return nameof(CircularProgressBar);
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.ProgressBar;
    }
}