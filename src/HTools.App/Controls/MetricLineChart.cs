using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace HTools.App.Controls;

public sealed class MetricLineChart : Control
{
    public static readonly StyledProperty<double[]> ValuesProperty =
        AvaloniaProperty.Register<MetricLineChart, double[]>(nameof(Values), []);

    public static readonly StyledProperty<double[]> SecondaryValuesProperty =
        AvaloniaProperty.Register<MetricLineChart, double[]>(nameof(SecondaryValues), []);

    public static readonly StyledProperty<Color> LineColorProperty =
        AvaloniaProperty.Register<MetricLineChart, Color>(nameof(LineColor), Color.Parse("#4C9EFF"));

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<MetricLineChart, double>(nameof(Maximum), 100);

    static MetricLineChart() => AffectsRender<MetricLineChart>(ValuesProperty, SecondaryValuesProperty, LineColorProperty, MaximumProperty);

    public double[] Values { get => GetValue(ValuesProperty); set => SetValue(ValuesProperty, value); }

    public double[] SecondaryValues { get => GetValue(SecondaryValuesProperty); set => SetValue(SecondaryValuesProperty, value); }

    public Color LineColor { get => GetValue(LineColorProperty); set => SetValue(LineColorProperty, value); }

    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = new Rect(Bounds.Size).Deflate(new Thickness(1, 8, 1, 8));
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(42, 150, 170, 195)), 1);
        for (var i = 1; i <= 3; i++)
        {
            var y = bounds.Top + bounds.Height * i / 4;
            context.DrawLine(gridPen, new Point(bounds.Left, y), new Point(bounds.Right, y));
        }
        var maximum = Maximum > 0
            ? Maximum
            : Math.Max(1, Math.Max(Values.DefaultIfEmpty().Max(), SecondaryValues.DefaultIfEmpty().Max()) * 1.15);
        DrawSeries(context, Values, LineColor, bounds, maximum);
        if (SecondaryValues.Length > 0)
            DrawSeries(context, SecondaryValues, Color.Parse("#ECA94F"), bounds, maximum);
    }

    private static void DrawSeries(DrawingContext context, IReadOnlyList<double> values, Color color, Rect bounds, double maximum)
    {
        if (values.Count == 0 || bounds.Width <= 0 || bounds.Height <= 0) return;
        var pen = new Pen(new SolidColorBrush(color), 2.2, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            var first = ToPoint(values[0], 0, values.Count, bounds, maximum);
            path.BeginFigure(first, false);
            for (var i = 1; i < values.Count; i++)
                path.LineTo(ToPoint(values[i], i, values.Count, bounds, maximum));
        }
        context.DrawGeometry(null, pen, geometry);
    }

    private static Point ToPoint(double value, int index, int count, Rect bounds, double maximum)
    {
        var x = count <= 1 ? bounds.Right : bounds.Left + bounds.Width * index / (count - 1);
        var y = bounds.Bottom - bounds.Height * Math.Clamp(value / maximum, 0, 1);
        return new Point(x, y);
    }
}
