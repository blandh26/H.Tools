using Avalonia;
using Avalonia.Controls;

namespace HTools.App.Controls;

/// <summary>Lays out cards in as many columns as fit while keeping every card at least the configured width.</summary>
public sealed class AdaptiveCardPanel : Panel
{
    public static readonly StyledProperty<double> MinItemWidthProperty =
        AvaloniaProperty.Register<AdaptiveCardPanel, double>(nameof(MinItemWidth), 220);

    public static readonly StyledProperty<double> ColumnSpacingProperty =
        AvaloniaProperty.Register<AdaptiveCardPanel, double>(nameof(ColumnSpacing), 12);

    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<AdaptiveCardPanel, double>(nameof(RowSpacing), 12);

    private readonly List<double> _rowHeights = [];
    private int _columns = 1;
    private double _cellWidth;

    static AdaptiveCardPanel()
    {
        AffectsMeasure<AdaptiveCardPanel>(MinItemWidthProperty, ColumnSpacingProperty, RowSpacingProperty);
    }

    public double MinItemWidth
    {
        get => GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public double ColumnSpacing
    {
        get => GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    public double RowSpacing
    {
        get => GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _rowHeights.Clear();
        if (Children.Count == 0)
        {
            return new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width, 0);
        }

        var minWidth = Math.Max(80, MinItemWidth);
        var columnGap = Math.Max(0, ColumnSpacing);
        var availableWidth = double.IsFinite(availableSize.Width)
            ? Math.Max(minWidth, availableSize.Width)
            : minWidth * Children.Count;

        _columns = Math.Clamp((int)Math.Floor((availableWidth + columnGap) / (minWidth + columnGap)), 1, Children.Count);
        _cellWidth = Math.Max(minWidth, (availableWidth - columnGap * (_columns - 1)) / _columns);

        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            child.Measure(new Size(_cellWidth, double.PositiveInfinity));
            var row = index / _columns;
            while (_rowHeights.Count <= row)
            {
                _rowHeights.Add(0);
            }

            _rowHeights[row] = Math.Max(_rowHeights[row], child.DesiredSize.Height);
        }

        var height = _rowHeights.Sum() + Math.Max(0, RowSpacing) * Math.Max(0, _rowHeights.Count - 1);
        return new Size(double.IsFinite(availableSize.Width) ? availableSize.Width : _cellWidth * _columns, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0)
        {
            return finalSize;
        }

        var minWidth = Math.Max(80, MinItemWidth);
        var columnGap = Math.Max(0, ColumnSpacing);
        _columns = Math.Clamp((int)Math.Floor((finalSize.Width + columnGap) / (minWidth + columnGap)), 1, Children.Count);
        _cellWidth = Math.Max(minWidth, (finalSize.Width - columnGap * (_columns - 1)) / _columns);

        var rowGap = Math.Max(0, RowSpacing);
        var rowY = 0d;
        for (var row = 0; row < _rowHeights.Count; row++)
        {
            var first = row * _columns;
            var last = Math.Min(first + _columns, Children.Count);
            for (var index = first; index < last; index++)
            {
                var column = index - first;
                Children[index].Arrange(new Rect(column * (_cellWidth + columnGap), rowY, _cellWidth, _rowHeights[row]));
            }

            rowY += _rowHeights[row] + rowGap;
        }

        return finalSize;
    }
}
