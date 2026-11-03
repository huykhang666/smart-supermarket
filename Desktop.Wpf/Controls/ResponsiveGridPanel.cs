using System;
using System.Windows;
using System.Windows.Controls;

namespace SmartSupermarket.POS.Controls;

/// <summary>
/// A responsive uniform grid panel that dynamically calculates the optimal number
/// of columns based on available width, ensuring all items stretch evenly to fill
/// the entire row with perfectly balanced left and right margins (no awkward gaps).
/// </summary>
public class ResponsiveGridPanel : Panel
{
    public static readonly DependencyProperty MinItemWidthProperty =
        DependencyProperty.Register(
            nameof(MinItemWidth),
            typeof(double),
            typeof(ResponsiveGridPanel),
            new FrameworkPropertyMetadata(195.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    public static readonly DependencyProperty ItemHeightProperty =
        DependencyProperty.Register(
            nameof(ItemHeight),
            typeof(double),
            typeof(ResponsiveGridPanel),
            new FrameworkPropertyMetadata(255.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    public double MinItemWidth
    {
        get => (double)GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        int count = InternalChildren.Count;
        if (count == 0) return new Size(0, 0);

        double width = availableSize.Width;
        if (double.IsPositiveInfinity(width) || double.IsNaN(width) || width <= 0)
        {
            width = 800;
        }

        int columns = Math.Max(1, (int)Math.Floor(width / MinItemWidth));
        double itemWidth = width / columns;
        double itemHeight = ItemHeight;

        Size childMeasureSize = new Size(itemWidth, itemHeight);
        foreach (UIElement child in InternalChildren)
        {
            child.Measure(childMeasureSize);
        }

        int rows = (int)Math.Ceiling((double)count / columns);
        return new Size(width, rows * itemHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int count = InternalChildren.Count;
        if (count == 0) return finalSize;

        double width = finalSize.Width;
        if (width <= 0) width = 800;

        int columns = Math.Max(1, (int)Math.Floor(width / MinItemWidth));
        double itemWidth = width / columns;
        double itemHeight = ItemHeight;

        for (int i = 0; i < count; i++)
        {
            int col = i % columns;
            int row = i / columns;

            Rect itemRect = new Rect(col * itemWidth, row * itemHeight, itemWidth, itemHeight);
            InternalChildren[i].Arrange(itemRect);
        }

        return finalSize;
    }
}
