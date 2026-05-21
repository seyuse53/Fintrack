using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FinTrack.Avalonia.Controls;

public class SparklinePoint
{
    public DateTime Date { get; set; }
    public decimal Close { get; set; }
    public decimal Low { get; set; }
    public decimal High { get; set; }
}

public class AvaloniaSparklineControl : Control
{
    // ── Styled Properties ────────────────────────────
    public static readonly StyledProperty<List<SparklinePoint>?> DataProperty =
        AvaloniaProperty.Register<AvaloniaSparklineControl, List<SparklinePoint>?>(nameof(Data));

    public static readonly StyledProperty<bool> ShowFillProperty =
        AvaloniaProperty.Register<AvaloniaSparklineControl, bool>(nameof(ShowFill), true);

    public static readonly StyledProperty<bool> ShowLabelsProperty =
        AvaloniaProperty.Register<AvaloniaSparklineControl, bool>(nameof(ShowLabels), true);

    public static readonly StyledProperty<bool> ShowTooltipProperty =
        AvaloniaProperty.Register<AvaloniaSparklineControl, bool>(nameof(ShowTooltip), true);

    public static readonly StyledProperty<IBrush?> LineColorOverrideProperty =
        AvaloniaProperty.Register<AvaloniaSparklineControl, IBrush?>(nameof(LineColorOverride));

    public List<SparklinePoint>? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public bool ShowFill
    {
        get => GetValue(ShowFillProperty);
        set => SetValue(ShowFillProperty, value);
    }

    public bool ShowLabels
    {
        get => GetValue(ShowLabelsProperty);
        set => SetValue(ShowLabelsProperty, value);
    }

    public bool ShowTooltip
    {
        get => GetValue(ShowTooltipProperty);
        set => SetValue(ShowTooltipProperty, value);
    }

    public IBrush? LineColorOverride
    {
        get => GetValue(LineColorOverrideProperty);
        set => SetValue(LineColorOverrideProperty, value);
    }

    // ── Computed state ──────────────────────────────────
    private List<Point> _screenPoints = new();
    private List<SparklinePoint> _sortedData = new();
    private double _chartLeft, _chartTop, _chartRight, _chartBottom;
    private decimal _minPrice, _maxPrice;
    private Point? _crosshairPoint;
    private SparklinePoint? _crosshairDataPoint;

    static AvaloniaSparklineControl()
    {
        AffectsRender<AvaloniaSparklineControl>(DataProperty, ShowFillProperty, ShowLabelsProperty, LineColorOverrideProperty);
        ClipToBoundsProperty.OverrideDefaultValue<AvaloniaSparklineControl>(true);
    }

    public AvaloniaSparklineControl()
    {
    }

    // ── Rendering ──────────────────────────────────────
    public override void Render(DrawingContext ctx)
    {
        base.Render(ctx);

        var data = Data;
        if (data == null || data.Count < 2 || Bounds.Width < 10 || Bounds.Height < 10)
        {
            return;
        }

        _sortedData = data.OrderBy(p => p.Date).ToList();
        _minPrice = _sortedData.Min(p => p.Low > 0 ? p.Low : p.Close);
        _maxPrice = _sortedData.Max(p => p.High > 0 ? p.High : p.Close);

        if (_maxPrice == _minPrice)
        {
            _maxPrice += 1;
            _minPrice = Math.Max(0, _minPrice - 1);
        }

        double labelMargin = ShowLabels ? 55 : 4;
        _chartLeft = 4;
        _chartTop = ShowLabels ? 20 : 4;
        _chartRight = Bounds.Width - labelMargin;
        _chartBottom = Bounds.Height - (ShowLabels ? 22 : 4);

        double chartW = _chartRight - _chartLeft;
        double chartH = _chartBottom - _chartTop;

        if (chartW < 10 || chartH < 10) return;

        _screenPoints.Clear();
        for (int i = 0; i < _sortedData.Count; i++)
        {
            double x = _chartLeft + (chartW * i / (_sortedData.Count - 1));
            double y = _chartBottom - (double)((_sortedData[i].Close - _minPrice) / (_maxPrice - _minPrice)) * chartH;
            _screenPoints.Add(new Point(x, y));
        }

        bool isPositive = _sortedData.Last().Close >= _sortedData.First().Close;
        var trendColor = isPositive ? Color.Parse("#27AE60") : Color.Parse("#E74C3C");

        IBrush lineBrush = LineColorOverride ?? new SolidColorBrush(trendColor);
        IPen linePen = new Pen(lineBrush, 2.0, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

        // Background (draw transparent rectangle to capture mouse events)
        ctx.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Bounds.Width, Bounds.Height));

        // ── Fill area (gradient) ──
        if (ShowFill && _screenPoints.Count >= 2)
        {
            var fillGeometry = new StreamGeometry();
            using (var sgc = fillGeometry.Open())
            {
                sgc.BeginFigure(new Point(_screenPoints[0].X, _chartBottom), true);
                for (int i = 0; i < _screenPoints.Count; i++)
                    sgc.LineTo(_screenPoints[i]);
                sgc.LineTo(new Point(_screenPoints[^1].X, _chartBottom));
                sgc.EndFigure(true);
            }

            var gradientStops = new GradientStops
            {
                new GradientStop(Color.FromArgb(60, trendColor.R, trendColor.G, trendColor.B), 0),
                new GradientStop(Color.FromArgb(5, trendColor.R, trendColor.G, trendColor.B), 1)
            };

            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops = gradientStops
            };

            ctx.DrawGeometry(gradientBrush, null, fillGeometry);
        }

        // ── Line ──
        var lineGeometry = new StreamGeometry();
        using (var sgc = lineGeometry.Open())
        {
            sgc.BeginFigure(_screenPoints[0], false);
            for (int i = 1; i < _screenPoints.Count; i++)
                sgc.LineTo(_screenPoints[i]);
            sgc.EndFigure(false);
        }
        ctx.DrawGeometry(null, linePen, lineGeometry);

        /*
        // ── Labels ──
        if (ShowLabels)
        {
            var culture = CultureInfo.GetCultureInfo("tr-TR");
            var typeface = new Typeface("Inter", FontStyle.Normal, FontWeight.SemiBold);
            var dateTypeface = new Typeface("Inter", FontStyle.Normal, FontWeight.Normal);

            var minText = new FormattedText($"₺{_minPrice:N2}", culture, FlowDirection.LeftToRight, typeface, 11, null);
            ctx.DrawText(minText, new Point(_chartRight + 5, _chartBottom - minText.Height / 2));

            var maxText = new FormattedText($"₺{_maxPrice:N2}", culture, FlowDirection.LeftToRight, typeface, 11, null);
            ctx.DrawText(maxText, new Point(_chartRight + 5, _chartTop - maxText.Height / 2));

            decimal lastPrice = _sortedData.Last().Close;
            var lastText = new FormattedText($"₺{lastPrice:N2}", culture, FlowDirection.LeftToRight, typeface, 11, null);
            double lastY = _screenPoints[^1].Y;
            ctx.DrawText(lastText, new Point(_chartRight + 5, lastY - lastText.Height / 2));

            var dashPen = new Pen(new SolidColorBrush(Color.FromArgb(80, trendColor.R, trendColor.G, trendColor.B)), 1, new DashStyle(new double[] { 2, 2 }, 0));
            ctx.DrawLine(dashPen, new Point(_chartLeft, lastY), new Point(_chartRight, lastY));

            var startDateText = new FormattedText(_sortedData.First().Date.ToString("dd.MM"), culture, FlowDirection.LeftToRight, dateTypeface, 10, null);
            ctx.DrawText(startDateText, new Point(_chartLeft, _chartBottom + 4));

            var endDateText = new FormattedText(_sortedData.Last().Date.ToString("dd.MM"), culture, FlowDirection.LeftToRight, dateTypeface, 10, null);
            ctx.DrawText(endDateText, new Point(_chartRight - endDateText.Width, _chartBottom + 4));
        }
        */

        if (_screenPoints.Count > 0)
        {
            var dotBrush = new SolidColorBrush(trendColor);
            ctx.DrawEllipse(dotBrush, null, _screenPoints[^1], 4, 4);
        }

        // ── Crosshair Tooltip Drawing ──
        if (ShowTooltip && _crosshairPoint.HasValue && _crosshairDataPoint != null)
        {
            var screenPoint = _crosshairPoint.Value;
            var crossPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)), 1, new DashStyle(new double[] { 2, 2 }, 0));
            ctx.DrawLine(crossPen, new Point(screenPoint.X, _chartTop), new Point(screenPoint.X, _chartBottom));

            bool isUp = _crosshairDataPoint.Close >= _sortedData.First().Close;
            var dotColor = isUp ? Color.Parse("#27AE60") : Color.Parse("#E74C3C");
            var brush = new SolidColorBrush(dotColor);
            ctx.DrawEllipse(brush, new Pen(Brushes.White, 2), screenPoint, 5, 5);

            // Using Avalonia's ToolTipService via attached property on mouse move instead of drawing text manually
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!ShowTooltip || _screenPoints.Count == 0 || _sortedData.Count == 0) return;

        var pos = e.GetPosition(this);

        int nearestIdx = 0;
        double nearestDist = double.MaxValue;
        for (int i = 0; i < _screenPoints.Count; i++)
        {
            double dist = Math.Abs(_screenPoints[i].X - pos.X);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestIdx = i;
            }
        }

        _crosshairDataPoint = _sortedData[nearestIdx];
        _crosshairPoint = _screenPoints[nearestIdx];

        string tooltipText = $"📅 {_crosshairDataPoint.Date:dd.MM.yyyy}\n" +
                             $"💰 Kapanış: ₺{_crosshairDataPoint.Close:N2}\n";
        
        if (_crosshairDataPoint.Low > 0) tooltipText += $"📉 En Düşük: ₺{_crosshairDataPoint.Low:N2}\n";
        if (_crosshairDataPoint.High > 0) tooltipText += $"📈 En Yüksek: ₺{_crosshairDataPoint.High:N2}";

        ToolTip.SetTip(this, tooltipText.TrimEnd());
        ToolTip.SetIsOpen(this, true);

        InvalidateVisual(); // Trigger redraw for crosshair
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _crosshairPoint = null;
        _crosshairDataPoint = null;
        ToolTip.SetIsOpen(this, false);
        InvalidateVisual();
    }
}
