using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FEx.WPFx.Controls.VirtualizingWrapPanelControl;

/// <summary>
/// A implementation of a wrap panel that supports virtualization and can be used in horizontal and vertical
/// orientation.
/// In addition the panel allows to expand one specific item.
/// <p class="note">In order to work properly all items must have the same size.</p>
/// </summary>
public class VirtualizingWrapPanelWithItemExpansion : VirtualizingWrapPanelV1
{
    public static readonly DependencyProperty ExpandedItemTemplateProperty =
        DependencyProperty.Register(nameof(ExpandedItemTemplate),
            typeof(DataTemplate),
            typeof(VirtualizingWrapPanelWithItemExpansion),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty ExpandedItemProperty = DependencyProperty.Register(nameof(ExpandedItem),
        typeof(object),
        typeof(VirtualizingWrapPanelWithItemExpansion),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsMeasure,
            (o, a) => ((VirtualizingWrapPanelWithItemExpansion)o).ExpandedItemPropertyChanged(a)));

    private FrameworkElement _expandedItemChild;

    private int _itemIndexFollwingExpansion;

    /// <summary>
    /// Gets or sets the data template used for the item expansion.
    /// </summary>
    public DataTemplate ExpandedItemTemplate
    {
        get => (DataTemplate)GetValue(ExpandedItemTemplateProperty);
        set => SetValue(ExpandedItemTemplateProperty, value);
    }

    /// <summary>
    /// Gets or set the expanded item. The default value is null.
    /// </summary>
    public object ExpandedItem
    {
        get => GetValue(ExpandedItemProperty);
        set => SetValue(ExpandedItemProperty, value);
    }

    private int ExpandedItemIndex => Items.IndexOf(ExpandedItem);

    protected override Size CalculateExtent(Size availableSize)
    {
        Size extent = base.CalculateExtent(availableSize);

        if (_expandedItemChild != null)
        {
            if (Orientation == Orientation.Horizontal)
                extent.Height += _expandedItemChild.DesiredSize.Height;
            else
                extent.Width += _expandedItemChild.DesiredSize.Width;
        }

        return extent;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double expandedItemChildHeight = 0;

        Size childSize = CalculateChildArrangeSize(finalSize);

        CalculateSpacing(finalSize, out double innerSpacing, out double outerSpacing);

        for (var childIndex = 0; childIndex < InternalChildren.Count; childIndex++)
        {
            UIElement child = InternalChildren[childIndex];

            if (child == _expandedItemChild)
            {
                int rowIndex = ExpandedItemIndex / _itemsPerRowCount + 1;
                double x = outerSpacing;
                double y = rowIndex * GetHeight(childSize);
                double width = GetWidth(finalSize) - 2 * outerSpacing;
                double height = GetHeight(_expandedItemChild.DesiredSize);

                if (SpacingMode == SpacingMode.None)
                    width = _itemsPerRowCount * GetWidth(childSize);

                if (Orientation == Orientation.Horizontal)
                    _expandedItemChild.Arrange(CreateRect(x - GetX(Offset), y - GetY(Offset), width, height));
                else
                    _expandedItemChild.Arrange(CreateRect(x - GetX(Offset), y - GetY(Offset), height, width));

                expandedItemChildHeight = height;
            }
            else
            {
                int itemIndex = GetItemIndexFromChildIndex(childIndex);

                int columnIndex = itemIndex % _itemsPerRowCount;
                int rowIndex = itemIndex / _itemsPerRowCount;

                double x = outerSpacing + columnIndex * (GetWidth(childSize) + innerSpacing);
                double y = rowIndex * GetHeight(childSize) + expandedItemChildHeight;

                child.Arrange(CreateRect(x - GetX(Offset), y - GetY(Offset), childSize.Width, childSize.Height));
            }
        }

        return finalSize;
    }

    protected override void RealizeItems()
    {
        GeneratorPosition startPosition = ItemContainerGenerator.GeneratorPositionFromIndex(ItemRange.StartIndex);

        int childIndex = startPosition.Offset == 0
            ? startPosition.Index
            : startPosition.Index + 1;

        int expandedItemIndex = Items.IndexOf(ExpandedItem);

        int itemIndexFollwingExpansion = expandedItemIndex != -1
            ? (expandedItemIndex / _itemsPerRowCount + 1) * _itemsPerRowCount - 1
            : -1;

        itemIndexFollwingExpansion = Math.Min(itemIndexFollwingExpansion, Items.Count - 1);

        if (itemIndexFollwingExpansion != _itemIndexFollwingExpansion
            && _expandedItemChild != null)
        {
            RemoveInternalChildRange(InternalChildren.IndexOf(_expandedItemChild), 1);
            _expandedItemChild = null;
        }

        using (ItemContainerGenerator.StartAt(startPosition, GeneratorDirection.Forward, true))
        {
            for (int itemIndex = ItemRange.StartIndex; itemIndex <= ItemRange.EndIndex; itemIndex++, childIndex++)
            {
                var child = (FrameworkElement)ItemContainerGenerator.GenerateNext(out bool isNewlyRealized);

                if (isNewlyRealized || /*recycling*/!InternalChildren.Contains(child))
                {
                    if (childIndex >= InternalChildren.Count)
                        AddInternalChild(child);
                    else
                        InsertInternalChild(childIndex, child);

                    ItemContainerGenerator.PrepareItemContainer(child);

                    if (ItemSize == Size.Empty)
                        child.Measure(CreateSize(GetWidth(ViewportSize), double.MaxValue));
                    else
                        child.Measure(ItemSize);
                }

                if (itemIndex == itemIndexFollwingExpansion
                    && ExpandedItemTemplate != null)
                {
                    if (_expandedItemChild == null)
                    {
                        _expandedItemChild = (FrameworkElement)ExpandedItemTemplate.LoadContent();
                        _expandedItemChild.DataContext = Items[expandedItemIndex];
                        _expandedItemChild.Measure(CreateSize(GetWidth(ViewportSize), double.MaxValue));
                    }

                    if (!InternalChildren.Contains(_expandedItemChild))
                    {
                        childIndex++;

                        if (childIndex >= InternalChildren.Count)
                            AddInternalChild(_expandedItemChild);
                        else
                            InsertInternalChild(childIndex, _expandedItemChild);
                    }
                }
            }

            _itemIndexFollwingExpansion = itemIndexFollwingExpansion;
        }
    }

    protected override void OnClearChildren()
    {
        base.OnClearChildren();
        _expandedItemChild = null;
    }

    protected override GeneratorPosition GetGeneratorPositionFromChildIndex(int childIndex)
    {
        int expandedItemChildIndex = InternalChildren.IndexOf(_expandedItemChild);

        if (expandedItemChildIndex != -1
            && childIndex > expandedItemChildIndex)
            return new(childIndex - 1, 0);

        return new(childIndex, 0);
    }

    protected override void VirtualizeItems()
    {
        for (int childIndex = InternalChildren.Count - 1; childIndex >= 0; childIndex--)
        {
            var child = (FrameworkElement)InternalChildren[childIndex];

            if (child == _expandedItemChild)
            {
                if (!ItemRange.Contains(ExpandedItemIndex))
                {
                    _expandedItemChild = null;
                    RemoveInternalChildRange(childIndex, 1);
                }
            }
            else
            {
                int itemIndex = Items.IndexOf(child.DataContext);

                GeneratorPosition position = ItemContainerGenerator.GeneratorPositionFromIndex(itemIndex);

                if (!ItemRange.Contains(itemIndex))
                {
                    if (IsRecycling)
                        ItemContainerGenerator.Recycle(position, 1);
                    else
                        ItemContainerGenerator.Remove(position, 1);

                    RemoveInternalChildRange(childIndex, 1);
                }
            }
        }
    }

    protected override void BringIndexIntoView(int index)
    {
        double offset = index / _itemsPerRowCount * GetHeight(_childSize);

        if (_expandedItemChild != null
            && index > _itemIndexFollwingExpansion)
            offset += GetHeight(_expandedItemChild.DesiredSize);

        if (Orientation == Orientation.Horizontal)
            SetHorizontalOffset(offset);
        else
            SetVerticalOffset(offset);
    }

    private void ExpandedItemPropertyChanged(DependencyPropertyChangedEventArgs args)
    {
        if (args.OldValue != null)
        {
            int index = InternalChildren.IndexOf(_expandedItemChild);

            if (index != -1)
            {
                _expandedItemChild = null;
                RemoveInternalChildRange(index, 1);
            }
        }
    }
}