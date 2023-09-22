using System.Windows;

namespace FEx.WPFx.Controls.VirtualizingWrapPanelControl;

internal interface IItemContainerInfo
{
    UIElement UIElement { get; }

    Size DesiredSize { get; }

    bool IsMeasureValid { get; }

    Size MaxSize { get; }

    object Item { get; }

    Size Measure(Size availableSize);

    void Arrange(Rect rect);
}

internal class ItemContainerInfo : IItemContainerInfo
{
    public UIElement UIElement { get; }

    public Size DesiredSize => UIElement.DesiredSize;

    public bool IsMeasureValid => UIElement.IsMeasureValid;

    public Size MaxSize { get; } = new(double.PositiveInfinity, double.PositiveInfinity);

    public object Item { get; }

    private ItemContainerInfo(UIElement uiElement, object item)
    {
        UIElement = uiElement;
        Item = item;

        if (uiElement is FrameworkElement fe)
            MaxSize = new Size(fe.MaxWidth, fe.MaxHeight);
        Item = item;
    }

    public Size Measure(Size availableSize)
    {
        UIElement.Measure(availableSize);
        return UIElement.DesiredSize;
    }

    public void Arrange(Rect rect)
    {
        UIElement.Arrange(rect);
    }

    public static IItemContainerInfo For(UIElement uiElement, object item) => new ItemContainerInfo(uiElement, item);

    public static bool operator ==(ItemContainerInfo obj1, ItemContainerInfo obj2) =>
        ReferenceEquals(obj1?.UIElement, obj2?.UIElement);

    public static bool operator !=(ItemContainerInfo obj1, ItemContainerInfo obj2) =>
        !ReferenceEquals(obj1?.UIElement, obj2?.UIElement);

    public override bool Equals(object obj) =>
        obj is ItemContainerInfo other && ReferenceEquals(UIElement, other.UIElement);

    public override int GetHashCode() => UIElement.GetHashCode();
}