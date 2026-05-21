using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FinTrack.WPF.Controls
{
    /// <summary>
    /// Veri noktası: tarih + kapanış + low + high
    /// </summary>
    public class SparklinePoint
    {
        public DateTime Date { get; set; }
        public decimal Close { get; set; }
        public decimal Low { get; set; }
        public decimal High { get; set; }
    }

    /// <summary>
    /// DrawingVisual tabanlı yüksek performanslı fiyat grafiği.
    /// Harici kütüphane gerektirmez — WPF'in DirectX hızlandırmalı çizim katmanını kullanır.
    /// </summary>
    public class SparklineControl : FrameworkElement
    {
        // ── Dependency Properties ────────────────────────────
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(List<SparklinePoint>), typeof(SparklineControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnDataChanged));

        public static readonly DependencyProperty ShowFillProperty =
            DependencyProperty.Register(nameof(ShowFill), typeof(bool), typeof(SparklineControl),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowLabelsProperty =
            DependencyProperty.Register(nameof(ShowLabels), typeof(bool), typeof(SparklineControl),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowTooltipProperty =
            DependencyProperty.Register(nameof(ShowTooltip), typeof(bool), typeof(SparklineControl),
                new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty LineColorOverrideProperty =
            DependencyProperty.Register(nameof(LineColorOverride), typeof(Brush), typeof(SparklineControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public List<SparklinePoint>? Data
        {
            get => (List<SparklinePoint>?)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public bool ShowFill
        {
            get => (bool)GetValue(ShowFillProperty);
            set => SetValue(ShowFillProperty, value);
        }

        public bool ShowLabels
        {
            get => (bool)GetValue(ShowLabelsProperty);
            set => SetValue(ShowLabelsProperty, value);
        }

        public bool ShowTooltip
        {
            get => (bool)GetValue(ShowTooltipProperty);
            set => SetValue(ShowTooltipProperty, value);
        }

        public Brush? LineColorOverride
        {
            get => (Brush?)GetValue(LineColorOverrideProperty);
            set => SetValue(LineColorOverrideProperty, value);
        }

        // ── Visual tree ────────────────────────────────────
        private readonly VisualCollection _visuals;
        private DrawingVisual _chartVisual;
        private DrawingVisual _tooltipVisual;
        private readonly ToolTip _toolTip;

        // ── Computed state ──────────────────────────────────
        private List<Point> _screenPoints = new();
        private List<SparklinePoint> _sortedData = new();
        private double _chartLeft, _chartTop, _chartRight, _chartBottom;
        private decimal _minPrice, _maxPrice;

        public SparklineControl()
        {
            _chartVisual = new DrawingVisual();
            _tooltipVisual = new DrawingVisual();
            _visuals = new VisualCollection(this) { _chartVisual, _tooltipVisual };
            _toolTip = new ToolTip { Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse };

            ClipToBounds = true;
        }

        protected override int VisualChildrenCount => _visuals.Count;
        protected override Visual GetVisualChild(int index) => _visuals[index];

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SparklineControl ctrl) ctrl.InvalidateVisual();
        }

        // ── Rendering ──────────────────────────────────────
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            DrawChart();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            DrawChart();
        }

        private void DrawChart()
        {
            var data = Data;
            if (data == null || data.Count < 2 || ActualWidth < 10 || ActualHeight < 10)
            {
                using var dc = _chartVisual.RenderOpen();
                // Empty — clear previous drawing
                return;
            }

            _sortedData = data.OrderBy(p => p.Date).ToList();
            _minPrice = _sortedData.Min(p => p.Low > 0 ? p.Low : p.Close);
            _maxPrice = _sortedData.Max(p => p.High > 0 ? p.High : p.Close);

            // Prevent flat line (identical prices)
            if (_maxPrice == _minPrice)
            {
                _maxPrice += 1;
                _minPrice = Math.Max(0, _minPrice - 1);
            }

            // Chart margins
            double labelMargin = ShowLabels ? 55 : 4;
            _chartLeft = 4;
            _chartTop = ShowLabels ? 20 : 4;
            _chartRight = ActualWidth - labelMargin;
            _chartBottom = ActualHeight - (ShowLabels ? 22 : 4);

            double chartW = _chartRight - _chartLeft;
            double chartH = _chartBottom - _chartTop;

            if (chartW < 10 || chartH < 10) return;

            // Map data points → screen coordinates
            _screenPoints = new List<Point>(_sortedData.Count);
            for (int i = 0; i < _sortedData.Count; i++)
            {
                double x = _chartLeft + (chartW * i / (_sortedData.Count - 1));
                double y = _chartBottom - (double)((_sortedData[i].Close - _minPrice) / (_maxPrice - _minPrice)) * chartH;
                _screenPoints.Add(new Point(x, y));
            }

            // Determine trend color
            bool isPositive = _sortedData.Last().Close >= _sortedData.First().Close;
            Color trendColor = isPositive
                ? (Color)ColorConverter.ConvertFromString("#27AE60")
                : (Color)ColorConverter.ConvertFromString("#E74C3C");

            Brush lineBrush = LineColorOverride ?? new SolidColorBrush(trendColor);
            var linePen = new Pen(lineBrush, 2.0) { LineJoin = PenLineJoin.Round };
            linePen.Freeze();

            using var ctx = _chartVisual.RenderOpen();

            // Background
            ctx.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));

            // ── Fill area (gradient) ──
            if (ShowFill && _screenPoints.Count >= 2)
            {
                var fillGeometry = new StreamGeometry();
                using (var sgc = fillGeometry.Open())
                {
                    sgc.BeginFigure(new Point(_screenPoints[0].X, _chartBottom), true, true);
                    for (int i = 0; i < _screenPoints.Count; i++)
                        sgc.LineTo(_screenPoints[i], false, false);
                    sgc.LineTo(new Point(_screenPoints[^1].X, _chartBottom), false, false);
                }
                fillGeometry.Freeze();

                var gradientBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(60, trendColor.R, trendColor.G, trendColor.B), 0),
                        new GradientStop(Color.FromArgb(5, trendColor.R, trendColor.G, trendColor.B), 1)
                    }
                };
                gradientBrush.Freeze();
                ctx.DrawGeometry(gradientBrush, null, fillGeometry);
            }

            // ── Line (StreamGeometry for perf) ──
            var lineGeometry = new StreamGeometry();
            using (var sgc = lineGeometry.Open())
            {
                sgc.BeginFigure(_screenPoints[0], false, false);
                for (int i = 1; i < _screenPoints.Count; i++)
                    sgc.LineTo(_screenPoints[i], true, false);
            }
            lineGeometry.Freeze();
            ctx.DrawGeometry(null, linePen, lineGeometry);

            // ── Labels ──
            if (ShowLabels)
            {
                var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
                var culture = CultureInfo.GetCultureInfo("tr-TR");

                // Min price
                var minText = new FormattedText($"₺{_minPrice:N2}", culture, FlowDirection.LeftToRight, typeface, 11, Brushes.Gray, 1.0);
                ctx.DrawText(minText, new Point(_chartRight + 5, _chartBottom - minText.Height / 2));

                // Max price
                var maxText = new FormattedText($"₺{_maxPrice:N2}", culture, FlowDirection.LeftToRight, typeface, 11, Brushes.Gray, 1.0);
                ctx.DrawText(maxText, new Point(_chartRight + 5, _chartTop - maxText.Height / 2));

                // Current price (last point)
                decimal lastPrice = _sortedData.Last().Close;
                var lastText = new FormattedText($"₺{lastPrice:N2}", culture, FlowDirection.LeftToRight, typeface, 11,
                    new SolidColorBrush(trendColor), 1.0);
                double lastY = _screenPoints[^1].Y;
                ctx.DrawText(lastText, new Point(_chartRight + 5, lastY - lastText.Height / 2));

                // Horizontal dashed line at current price
                var dashPen = new Pen(new SolidColorBrush(Color.FromArgb(80, trendColor.R, trendColor.G, trendColor.B)), 1)
                {
                    DashStyle = DashStyles.Dash
                };
                dashPen.Freeze();
                ctx.DrawLine(dashPen, new Point(_chartLeft, lastY), new Point(_chartRight, lastY));

                // Date labels
                var dateTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                var startDate = new FormattedText(_sortedData.First().Date.ToString("dd.MM"), culture, FlowDirection.LeftToRight, dateTypeface, 10, Brushes.Gray, 1.0);
                ctx.DrawText(startDate, new Point(_chartLeft, _chartBottom + 4));

                var endDate = new FormattedText(_sortedData.Last().Date.ToString("dd.MM"), culture, FlowDirection.LeftToRight, dateTypeface, 10, Brushes.Gray, 1.0);
                ctx.DrawText(endDate, new Point(_chartRight - endDate.Width, _chartBottom + 4));
            }

            // ── Last point dot ──
            if (_screenPoints.Count > 0)
            {
                var dotBrush = new SolidColorBrush(trendColor);
                dotBrush.Freeze();
                ctx.DrawEllipse(dotBrush, null, _screenPoints[^1], 4, 4);
            }
        }

        // ── Mouse tooltip ──────────────────────────────────
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!ShowTooltip || _screenPoints.Count == 0 || _sortedData.Count == 0) return;

            var pos = e.GetPosition(this);

            // Find nearest point
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

            var dataPoint = _sortedData[nearestIdx];
            var screenPoint = _screenPoints[nearestIdx];

            // Draw crosshair
            using var dc = _tooltipVisual.RenderOpen();
            var crossPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)), 1) { DashStyle = DashStyles.Dot };
            crossPen.Freeze();
            dc.DrawLine(crossPen, new Point(screenPoint.X, _chartTop), new Point(screenPoint.X, _chartBottom));

            // Highlight dot
            bool isUp = dataPoint.Close >= _sortedData.First().Close;
            var dotColor = isUp ? (Color)ColorConverter.ConvertFromString("#27AE60") : (Color)ColorConverter.ConvertFromString("#E74C3C");
            var brush = new SolidColorBrush(dotColor);
            brush.Freeze();
            dc.DrawEllipse(brush, new Pen(Brushes.White, 2), screenPoint, 5, 5);

            // Tooltip
            _toolTip.Content = $"📅 {dataPoint.Date:dd.MM.yyyy}\n" +
                               $"💰 Kapanış: ₺{dataPoint.Close:N2}\n" +
                               (dataPoint.Low > 0 ? $"📉 En Düşük: ₺{dataPoint.Low:N2}\n" : "") +
                               (dataPoint.High > 0 ? $"📈 En Yüksek: ₺{dataPoint.High:N2}" : "");
            _toolTip.IsOpen = true;
            ToolTip = _toolTip;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            _toolTip.IsOpen = false;
            using var dc = _tooltipVisual.RenderOpen();
            // Clear crosshair
        }
    }
}
