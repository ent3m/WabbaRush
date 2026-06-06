using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace WabbajackDownloader.Features.Dashboard;

/// <summary>
/// An uniform grid panel that automatically determines the number of rows and columns
/// based on the number of child elements and a specified <see cref="MaxColumnsProperty"/>.
/// </summary>
internal sealed class AutoGrid : Panel
{
    /// <summary>
    /// Defines the <see cref="MaxColumns"/> property.
    /// </summary>
    public static readonly StyledProperty<int> MaxColumnsProperty =
        AvaloniaProperty.Register<AutoGrid, int>(nameof(MaxColumns));

    /// <summary>
    /// Defines the <see cref="RowSpacing"/> property.
    /// </summary>
    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<AutoGrid, double>(nameof(RowSpacing), 0);

    /// <summary>
    /// Defines the <see cref="ColumnSpacing"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ColumnSpacingProperty =
        AvaloniaProperty.Register<AutoGrid, double>(nameof(ColumnSpacing), 0);

    private int _rows;
    private int _columns;

    static AutoGrid()
    {
        AffectsMeasure<AutoGrid>(MaxColumnsProperty, RowSpacingProperty, ColumnSpacingProperty);
    }

    /// <summary>
    /// Specifies the maximum number of columns. It is always greater than 0.
    /// </summary>
    /// <remarks>
    /// The actual number of columns is determined by the number of child elements and <see cref="MaxColumns"/>,
    /// and is always less than or equal to <see cref="MaxColumns"/>.
    /// If there are more children than <see cref="MaxColumns"/>, additional rows will be created to accommodate the children.
    /// </remarks>
    public int MaxColumns
    {
        get => GetValue(MaxColumnsProperty);
        set => SetValue(MaxColumnsProperty, value);
    }

    /// <summary>
    /// Specifies the spacing between rows.
    /// </summary>
    public double RowSpacing
    {
        get => GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    /// <summary>
    /// Specifies the spacing between columns.
    /// </summary>
    public double ColumnSpacing
    {
        get => GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        UpdateRowsAndColumns();

        // Skip calculation if there are no children
        if (_rows == 0 || _columns == 0)
            return new Size(0, 0);

        var maxWidth = 0d;
        var maxHeight = 0d;

        var childAvailableSize = new Size(
            Math.Max((availableSize.Width - (_columns - 1) * ColumnSpacing) / _columns, 0),
            Math.Max((availableSize.Height - (_rows - 1) * RowSpacing) / _rows, 0));

        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;

            child.Measure(childAvailableSize);

            if (child.DesiredSize.Width > maxWidth)
            {
                maxWidth = child.DesiredSize.Width;
            }

            if (child.DesiredSize.Height > maxHeight)
            {
                maxHeight = child.DesiredSize.Height;
            }
        }

        if (UseLayoutRounding)
        {
            var scale = LayoutHelper.GetLayoutScale(this);
            maxWidth = LayoutHelper.RoundLayoutValue(maxWidth, scale);
            maxHeight = LayoutHelper.RoundLayoutValue(maxHeight, scale);
        }

        var totalWidth = maxWidth * _columns + ColumnSpacing * (_columns - 1);
        var totalHeight = maxHeight * _rows + RowSpacing * (_rows - 1);

        totalWidth = Math.Max(totalWidth, 0);
        totalHeight = Math.Max(totalHeight, 0);

        return new Size(totalWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Skip calculation if there are no children
        if (_rows == 0 || _columns == 0)
            return finalSize;

        var x = 0;
        var y = 0;

        var columnSpacing = ColumnSpacing;
        var rowSpacing = RowSpacing;

        var width = Math.Max((finalSize.Width - (_columns - 1) * columnSpacing) / _columns, 0);
        var height = Math.Max((finalSize.Height - (_rows - 1) * rowSpacing) / _rows, 0);

        // If layout rounding is enabled, round the per-cell unit size to integral device units.
        if (UseLayoutRounding)
        {
            var scale = LayoutHelper.GetLayoutScale(this);
            width = LayoutHelper.RoundLayoutValue(width, scale);
            height = LayoutHelper.RoundLayoutValue(height, scale);
        }

        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }

            var rect = new Rect(
                x * (width + columnSpacing),
                y * (height + rowSpacing),
                width,
                height);

            child.Arrange(rect);

            x++;

            if (x >= _columns)
            {
                x = 0;
                y++;
            }
        }

        return finalSize;
    }

    private void UpdateRowsAndColumns()
    {
        // Cache MaxColumns to avoid hitting the dependency property storage repeatedly
        int maxCol = MaxColumns;
        if (maxCol < 1)
        {
            maxCol = 1;
            SetCurrentValue(MaxColumnsProperty, 1);
        }

        var itemCount = 0;
        foreach (var child in Children)
        {
            if (child.IsVisible)
            {
                itemCount++;
            }
        }

        _rows = itemCount / maxCol + (itemCount % maxCol > 0 ? 1 : 0);
        _columns = itemCount < maxCol ? itemCount : maxCol;
    }
}
