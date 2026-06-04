using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ARNetDiscovery.Wpf.Controls;

/// <summary>
/// Lightweight auto-arrangement panel for ARNet device cards.
/// It keeps cards centered, balanced per row, and spreads them left/right as the canvas grows.
/// No external graph-layout dependency is used so the app stays small and portable.
/// </summary>
public sealed class FluidNetworkPanel : Panel
{
    private readonly List<Rect> _arrangedRects = new();

    public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register(
        nameof(ItemWidth), typeof(double), typeof(FluidNetworkPanel),
        new FrameworkPropertyMetadata(220d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(
        nameof(ItemHeight), typeof(double), typeof(FluidNetworkPanel),
        new FrameworkPropertyMetadata(150d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MinColumnGapProperty = DependencyProperty.Register(
        nameof(MinColumnGap), typeof(double), typeof(FluidNetworkPanel),
        new FrameworkPropertyMetadata(24d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty RowGapProperty = DependencyProperty.Register(
        nameof(RowGap), typeof(double), typeof(FluidNetworkPanel),
        new FrameworkPropertyMetadata(28d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TopPaddingProperty = DependencyProperty.Register(
        nameof(TopPadding), typeof(double), typeof(FluidNetworkPanel),
        new FrameworkPropertyMetadata(34d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MaxColumnsProperty = DependencyProperty.Register(
        nameof(MaxColumns), typeof(int), typeof(FluidNetworkPanel),
        new FrameworkPropertyMetadata(7, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty DrawConnectorsProperty = DependencyProperty.Register(
        nameof(DrawConnectors), typeof(bool), typeof(FluidNetworkPanel),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    public double MinColumnGap
    {
        get => (double)GetValue(MinColumnGapProperty);
        set => SetValue(MinColumnGapProperty, value);
    }

    public double RowGap
    {
        get => (double)GetValue(RowGapProperty);
        set => SetValue(RowGapProperty, value);
    }

    public double TopPadding
    {
        get => (double)GetValue(TopPaddingProperty);
        set => SetValue(TopPaddingProperty, value);
    }

    public int MaxColumns
    {
        get => (int)GetValue(MaxColumnsProperty);
        set => SetValue(MaxColumnsProperty, value);
    }

    public bool DrawConnectors
    {
        get => (bool)GetValue(DrawConnectorsProperty);
        set => SetValue(DrawConnectorsProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var count = InternalChildren.Count;
        if (count == 0)
            return new Size(0, TopPadding + 16);

        foreach (UIElement child in InternalChildren)
            child.Measure(new Size(ItemWidth, ItemHeight));

        var width = NormalizeWidth(availableSize.Width);
        var columns = CalculateColumns(width, count);
        var rows = (int)Math.Ceiling(count / (double)columns);
        var desiredHeight = TopPadding + rows * ItemHeight + Math.Max(0, rows - 1) * RowGap + 18;
        var desiredWidth = double.IsInfinity(availableSize.Width)
            ? columns * ItemWidth + Math.Max(0, columns - 1) * MinColumnGap
            : availableSize.Width;

        return new Size(Math.Max(0, desiredWidth), Math.Max(0, desiredHeight));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _arrangedRects.Clear();
        var count = InternalChildren.Count;
        if (count == 0)
            return finalSize;

        var width = NormalizeWidth(finalSize.Width);
        var columns = CalculateColumns(width, count);
        var rowCount = (int)Math.Ceiling(count / (double)columns);
        var baseItemsPerRow = count / rowCount;
        var extraRows = count % rowCount;
        var childIndex = 0;

        for (var row = 0; row < rowCount; row++)
        {
            var itemsInRow = baseItemsPerRow + (row < extraRows ? 1 : 0);
            var rowWidth = itemsInRow * ItemWidth + Math.Max(0, itemsInRow - 1) * MinColumnGap;
            var startX = Math.Max(0, (finalSize.Width - rowWidth) / 2.0);
            var y = TopPadding + row * (ItemHeight + RowGap);

            // Gentle stagger gives a topology-map feeling without creating a hard-to-read random graph.
            if (row > 0 && row % 2 == 1 && itemsInRow < columns)
                startX += Math.Min(MinColumnGap / 2.0, Math.Max(0, (finalSize.Width - rowWidth) / 4.0));

            for (var col = 0; col < itemsInRow && childIndex < count; col++, childIndex++)
            {
                var rect = new Rect(startX + col * (ItemWidth + MinColumnGap), y, ItemWidth, ItemHeight);
                InternalChildren[childIndex].Arrange(rect);
                _arrangedRects.Add(rect);
            }
        }

        InvalidateVisual();
        return finalSize;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (!DrawConnectors || _arrangedRects.Count == 0)
            return;

        // Soft cyan connector line, frozen for performance.
        var lineBrush = new SolidColorBrush(Color.FromRgb(0x6F, 0xC7, 0xE0)) { Opacity = 0.55 };
        lineBrush.Freeze();
        var pen = new Pen(lineBrush, 1.4)
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        pen.Freeze();

        // Hub marker at top-center (matches the UX reference dot above the topology).
        var hub = new Point(RenderSize.Width / 2.0, 6);
        var hubBrush = new SolidColorBrush(Color.FromRgb(0x20, 0xA9, 0xD8));
        hubBrush.Freeze();
        var hubHaloBrush = new SolidColorBrush(Color.FromRgb(0xBC, 0xEA, 0xF6)) { Opacity = 0.65 };
        hubHaloBrush.Freeze();
        dc.DrawEllipse(hubHaloBrush, null, hub, 9, 9);
        dc.DrawEllipse(hubBrush, null, hub, 4.5, 4.5);

        foreach (var rect in _arrangedRects)
        {
            var target = new Point(rect.Left + rect.Width / 2.0, rect.Top + 4);
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(hub, false, false);
                var dy = target.Y - hub.Y;
                var c1 = new Point(hub.X, hub.Y + Math.Max(24, dy * 0.45));
                var c2 = new Point(target.X, target.Y - Math.Max(28, dy * 0.45));
                ctx.BezierTo(c1, c2, target, true, false);
            }
            geometry.Freeze();
            dc.DrawGeometry(null, pen, geometry);

            // Small accent dot where the line meets the node top.
            dc.DrawEllipse(hubBrush, null, target, 3, 3);
        }
    }

    private int CalculateColumns(double width, int itemCount)
    {
        if (itemCount <= 0)
            return 1;

        var usable = Math.Max(ItemWidth, width - 8);
        var possible = (int)Math.Floor((usable + MinColumnGap) / (ItemWidth + MinColumnGap));
        possible = Math.Max(1, possible);
        possible = Math.Min(possible, Math.Max(1, MaxColumns));
        return Math.Min(possible, itemCount);
    }

    private double NormalizeWidth(double width)
    {
        if (double.IsInfinity(width) || double.IsNaN(width) || width <= 0)
            return Math.Max(ItemWidth, 1040);
        return width;
    }
}
