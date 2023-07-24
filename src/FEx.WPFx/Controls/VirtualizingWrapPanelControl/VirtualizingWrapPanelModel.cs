using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FEx.WPFx.Controls.VirtualizingWrapPanelControl;

internal class VirtualizingWrapPanelModel : VirtualizingPanelModelBase
{
    private static readonly Size FallbackSize = new(48, 48);

    private readonly IItemContainerManager _itemContainerManager;
    private readonly IChildrenCollection _childrenCollection;

    private readonly List<object> _items;
    private readonly Dictionary<object, Size> _itemSizesCache = new();

    private Size? _sizeOfFirstItem;
    private Size? _averageItemSizeCache;

    private int _itemsInKnownExtend;

    private int _startItemIndex = -1;
    private int _endItemIndex = -1;

    private double _startItemOffsetX;
    private double _startItemOffsetY;

    private double _knownExtendX;
    private double _knownExtendY;

    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    public Size FixedItemSize { get; set; } = Size.Empty;
    public IItemSizeProvider ItemSizeProvider { get; set; } = null;
    public bool AllowDifferentSizedItems { get; set; } = false;

    public bool StretchItems { get; set; }
    public SpacingMode SpacingMode { get; set; }

    public VirtualizingWrapPanelModel(IItemContainerManager itemContainerManager,
                                      IChildrenCollection childrenCollection)
    {
        _itemContainerManager = itemContainerManager;
        _childrenCollection = childrenCollection;
        itemContainerManager.ItemsChanged += ItemContainerManager_ItemsChanged;
        _items = new List<object>(itemContainerManager.Items);
    }

    public Size OnMeasure(Size availableSize) => OnMeasure(availableSize, availableSize, ScrollOffset);

    public Size OnMeasure(Size availableSize, Size viewportSize, Point scrollOffset)
    {
        var invalidateScrollInfo = false;
        ScrollOffset = scrollOffset;
        _averageItemSizeCache = null;

        UpdateViewport(viewportSize, ref invalidateScrollInfo);
        FindStartIndexAndOffset();
        VirtualizeItemsBeforeStartIndex();
        RealizeItemsAndFindEndIndex();
        VirtualizeItemsAfterEndIndex();
        UpdateExtent(ref invalidateScrollInfo);

        if (invalidateScrollInfo)
            InvalidateScrollInfo();

        double desiredWidth = Math.Min(GetWidth(availableSize), GetWidth(Extent));
        double desiredHeight = Math.Min(GetHeight(availableSize), GetHeight(Extent));
        return CreateSize(desiredWidth, desiredHeight);
    }

    public Size OnArrange(Size finalSize, bool hierarchical)
    {
        foreach (IItemContainerInfo cachedContainer in _itemContainerManager.CachedContainers)
            cachedContainer.Arrange(new Rect(0, 0, 0, 0));

        double x = _startItemOffsetX + GetX(ScrollOffset);
        double y = hierarchical
            ? _startItemOffsetY
            : _startItemOffsetY - GetY(ScrollOffset);
        double rowHeight = 0;
        var rowChilds = new List<IItemContainerInfo>();
        var childSizes = new List<Size>();

        foreach (IItemContainerInfo child in _itemContainerManager.RealizedContainers.OrderBy(container =>
                     _itemContainerManager.FindItemIndexOfContainer(container)))
        {
            Size? upfrontKnownItemSize = GetUpfrontKnownItemSize(child.Item);

            Size childSize = upfrontKnownItemSize ?? _itemSizesCache[child.Item];

            if (x != 0
                && x + GetWidth(childSize) > GetWidth(finalSize))
            {
                ArrangeRow(GetWidth(finalSize), rowChilds, childSizes, y, hierarchical);
                x = 0;
                y += rowHeight;
                rowHeight = 0;
                rowChilds.Clear();
                childSizes.Clear();
            }

            x += GetWidth(childSize);
            rowHeight = Math.Max(rowHeight, GetHeight(childSize));
            rowChilds.Add(child);
            childSizes.Add(childSize);
        }

        if (rowChilds.Any())
            ArrangeRow(GetWidth(finalSize), rowChilds, childSizes, y, hierarchical);

        return finalSize;
    }

    public Size GetAverageItemSize()
    {
        if (FixedItemSize != Size.Empty)
            return FixedItemSize;

        if (!AllowDifferentSizedItems)
            return _sizeOfFirstItem ?? FallbackSize;

        if (_averageItemSizeCache is null
            && _itemSizesCache.Values.Any())
            _averageItemSizeCache = CalculateAverageSize(_itemSizesCache.Values);
        return _averageItemSizeCache ?? FallbackSize;
    }

    public void BringIndexIntoView(int itemIndex)
    {
        if (itemIndex < 0
            || itemIndex >= _items.Count)
            throw new ArgumentOutOfRangeException(nameof(itemIndex),
                $"The argument {nameof(itemIndex)} must be >= 0 and < the count of items.");

        Point itemOffset = FindItemOffset(itemIndex);

        if (GetY(itemOffset) < GetY(ScrollOffset)
            || GetY(itemOffset) + GetHeight(GetAssumedItemSize(_items[itemIndex])) > GetY(ScrollOffset))
        {
            if (Orientation == Orientation.Horizontal)
                SetVerticalOffset(GetY(itemOffset));
            else
                SetHorizontalOffset(GetY(itemOffset));
        }
    }

    private void ItemContainerManager_ItemsChanged(object sender, ItemContainerManagerItemsChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Remove
            || e.Action == NotifyCollectionChangedAction.Replace)
        {
            foreach (object item in _items.Except(_itemContainerManager.Items))
                _itemSizesCache.Remove(item);
            if (!_itemContainerManager.IsRecycling)
                foreach (IItemContainerInfo container in e.RemovedContainers)
                    _childrenCollection.RemoveChild(container);
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _itemSizesCache.Clear();
            // childrenCollection is cleared automatically
        }

        _itemsInKnownExtend = 0; // force recalucaltion of extend

        _items.Clear();
        _items.AddRange(_itemContainerManager.Items);
    }

    private Point FindItemOffset(int itemIndex)
    {
        double x = 0, y = 0, rowHeight = 0;

        for (var i = 0; i <= itemIndex; i++)
        {
            Size itemSize = GetAssumedItemSize(_items[i]);

            if (x + GetWidth(itemSize) > GetWidth(ViewportSize))
            {
                x = 0;
                y += rowHeight;
                rowHeight = 0;
            }

            if (i != itemIndex)
            {
                x += GetWidth(itemSize);
                rowHeight = Math.Max(rowHeight, GetHeight(itemSize));
            }
        }

        return CreatePoint(x, y);
    }


    private void UpdateViewport(Size availableSize, ref bool invalidateScrollInfo)
    {
        bool viewportChanged = availableSize != ViewportSize;

        ViewportSize = availableSize;

        if (viewportChanged)
            invalidateScrollInfo = true;
    }

    private void FindStartIndexAndOffset()
    {
        double startOffsetY = DetermineStartOffsetY();

        if (startOffsetY <= 0)
        {
            _startItemIndex = 0;
            _startItemOffsetX = 0;
            _startItemOffsetY = 0;
            return;
        }

        double x = 0, y = 0, rowHeight = 0;
        var indexOfFirstRowItem = 0;

        var itemIndex = 0;
        foreach (object item in _items) // foreach seems to be faster than a for loop
        {
            Size itemSize = GetAssumedItemSize(item);

            if (x + GetWidth(itemSize) > GetWidth(ViewportSize)
                && x != 0)
            {
                x = 0;
                y += rowHeight;
                rowHeight = 0;
                indexOfFirstRowItem = itemIndex;
            }

            x += GetWidth(itemSize);
            rowHeight = Math.Max(rowHeight, GetHeight(itemSize));

            if (y + rowHeight >= startOffsetY)
            {
                if (CacheLengthUnit == VirtualizationCacheLengthUnit.Item)
                {
                    _startItemIndex = Math.Max(indexOfFirstRowItem - (int)CacheLength.CacheBeforeViewport, 0);
                    Point itemOffset = FindItemOffset(_startItemIndex);
                    _startItemOffsetX = GetX(itemOffset);
                    _startItemOffsetY = GetY(itemOffset);
                }
                else
                {
                    _startItemIndex = indexOfFirstRowItem;
                    _startItemOffsetX = 0;
                    _startItemOffsetY = y;
                }

                break;
            }

            itemIndex++;
        }
    }

    private void RealizeItemsAndFindEndIndex()
    {
        if (_startItemIndex == -1)
        {
            _endItemIndex = -1;
            _knownExtendX = 0;
            _knownExtendY = 0;
            _itemsInKnownExtend = 0;
            return;
        }

        int newEndItemIndex = _items.Count - 1;

        double endOffsetY = DetermineEndOffsetY();

        double x = _startItemOffsetX;
        double y = _startItemOffsetY;
        double rowHeight = 0;

        _knownExtendX = 0;

        for (int itemIndex = _startItemIndex, childIndex = 0; itemIndex < _items.Count; itemIndex++, childIndex++)
        {
            if (!AllowDifferentSizedItems
                && itemIndex == 0)
                _sizeOfFirstItem = null;

            object item = _items[itemIndex];

            IItemContainerInfo container =
                _itemContainerManager.Realize(itemIndex, out bool _, out bool isNewContainer);
            if (isNewContainer)
                _childrenCollection.AddChild(container);

            Size? upfrontKnownItemSize = GetUpfrontKnownItemSize(item);

            if (!container.IsMeasureValid)
            {
                Size availableSize = upfrontKnownItemSize ?? new Size(double.PositiveInfinity, double.PositiveInfinity);
                container.Measure(availableSize);
            }

            Size containerSize = DetermineContainerSize(container, upfrontKnownItemSize);

            if (!AllowDifferentSizedItems
                && _sizeOfFirstItem == null)
                _sizeOfFirstItem = containerSize;

            if (x + GetWidth(containerSize) > GetWidth(ViewportSize)
                && x != 0)
            {
                x = 0;
                y += rowHeight;
                rowHeight = 0;
            }

            x += GetWidth(containerSize);
            _knownExtendX = Math.Max(x, _knownExtendX);
            rowHeight = Math.Max(rowHeight, GetHeight(containerSize));

            if (newEndItemIndex == _items.Count - 1)
            {
                if (!AllowDifferentSizedItems
                    && itemIndex + 1 < _items.Count
                    && x + _sizeOfFirstItem!.Value.Width > GetWidth(ViewportSize)
                    && y + rowHeight >= endOffsetY)
                    newEndItemIndex = itemIndex;
                else if (y >= endOffsetY)
                    newEndItemIndex = itemIndex;

                if (newEndItemIndex != _items.Count - 1
                    && CacheLengthUnit == VirtualizationCacheLengthUnit.Item)
                    newEndItemIndex = Math.Min(newEndItemIndex + (int)CacheLength.CacheAfterViewport, _items.Count - 1);
            }

            if (itemIndex >= newEndItemIndex)
                break;
        }

        _endItemIndex = newEndItemIndex;
        _knownExtendY = Math.Max(y + rowHeight, _knownExtendY);
        _itemsInKnownExtend = Math.Max(_endItemIndex + 1, _itemsInKnownExtend);
    }

    private Size DetermineContainerSize(IItemContainerInfo container, Size? upfrontKnownItemSize)
    {
        if (AllowDifferentSizedItems)
        {
            if (upfrontKnownItemSize is not null)
                return upfrontKnownItemSize.Value;
            _itemSizesCache[container.Item] = container.DesiredSize;
            return container.DesiredSize;
        }

        return upfrontKnownItemSize ?? container.DesiredSize;
    }

    private void VirtualizeItemsBeforeStartIndex()
    {
        var containers = _itemContainerManager.RealizedContainers.ToList();
        foreach (IItemContainerInfo container in containers)
        {
            int itemIndex = _itemContainerManager.FindItemIndexOfContainer(container);

            if (itemIndex < _startItemIndex)
                Virtualize(container);
        }
    }

    private void VirtualizeItemsAfterEndIndex()
    {
        var containers = _itemContainerManager.RealizedContainers.ToList();
        foreach (IItemContainerInfo container in containers)
        {
            int itemIndex = _itemContainerManager.FindItemIndexOfContainer(container);

            if (itemIndex > _endItemIndex)
                Virtualize(container);
        }
    }

    private void Virtualize(IItemContainerInfo container)
    {
        if (_itemContainerManager.Virtualize(container))
            _childrenCollection.RemoveChild(container);
    }

    private void UpdateExtent(ref bool invalidateScrollInfo)
    {
        Size extent;

        if (!AllowDifferentSizedItems)
        {
            extent = FixedItemSize != Size.Empty
                ? CalculateExtentForSameSizeItems(FixedItemSize)
                : CalculateExtentForSameSizeItems(_sizeOfFirstItem ?? FallbackSize);
        }
        else
        {
            if (_itemsInKnownExtend == 0)
            {
                extent = CalculateExtentForSameSizeItems(FallbackSize);
            }
            else
            {
                double estimatedExtend = (double)_items.Count / _itemsInKnownExtend * _knownExtendY;
                extent = CreateSize(_knownExtendX, estimatedExtend);
            }
        }

        if (extent != Extent)
        {
            Extent = extent;
            invalidateScrollInfo = true;
        }

        if (GetY(ScrollOffset) + GetHeight(ViewportSize) > GetHeight(Extent))
        {
            ScrollOffset = CreatePoint(GetX(ScrollOffset), Math.Max(0, GetHeight(Extent) - GetHeight(ViewportSize)));
            invalidateScrollInfo = true;
        }
    }

    private Size CalculateExtentForSameSizeItems(Size itemSize)
    {
        var itemsPerRow = (int)Math.Max(1, Math.Floor(GetWidth(ViewportSize) / GetWidth(itemSize)));
        double extentY = Math.Ceiling((double)_items.Count / itemsPerRow) * GetHeight(itemSize);
        return CreateSize(_knownExtendX, extentY);
    }

    private double DetermineStartOffsetY()
    {
        double cacheLength = 0;

        if (CacheLengthUnit == VirtualizationCacheLengthUnit.Page)
            cacheLength = CacheLength.CacheBeforeViewport * GetHeight(ViewportSize);
        else if (CacheLengthUnit == VirtualizationCacheLengthUnit.Pixel)
            cacheLength = CacheLength.CacheBeforeViewport;

        return Math.Max(GetY(ScrollOffset) - cacheLength, 0);
    }

    private double DetermineEndOffsetY()
    {
        double cacheLength = 0;

        if (CacheLengthUnit == VirtualizationCacheLengthUnit.Page)
            cacheLength = CacheLength.CacheAfterViewport * GetHeight(ViewportSize);
        else if (CacheLengthUnit == VirtualizationCacheLengthUnit.Pixel)
            cacheLength = CacheLength.CacheAfterViewport;

        return Math.Max(GetY(ScrollOffset), 0) + GetHeight(ViewportSize) + cacheLength;
    }

    private Size? GetUpfrontKnownItemSize(object item)
    {
        if (FixedItemSize != Size.Empty)
            return FixedItemSize;
        if (!AllowDifferentSizedItems
            && _sizeOfFirstItem != null)
            return _sizeOfFirstItem;
        if (ItemSizeProvider != null)
        {
            Size size = ItemSizeProvider.GetSizeForItem(item);
            _itemSizesCache[item] = size;
            return size;
        }

        return null;
    }

    private Size GetAssumedItemSize(object item)
    {
        if (GetUpfrontKnownItemSize(item) is Size upfrontKnownItemSize)
            return upfrontKnownItemSize;

        if (_itemSizesCache.TryGetValue(item, out Size cachedItemSize))
            return cachedItemSize;

        return GetAverageItemSize();
    }

    private void ArrangeRow(double rowWidth,
                            List<IItemContainerInfo> children,
                            List<Size> childSizes,
                            double y,
                            bool hierarchical)
    {
        double extraWidth = 0;
        double innerSpacing = 0;
        double outerSpacing = 0;

        if (StretchItems) // TODO: handle MaxWidth/MaxHeight and apply spacing
        {
            double summedUpChildWidth = childSizes.Sum(GetWidth);
            double unusedWidth = rowWidth - summedUpChildWidth;
            extraWidth = unusedWidth / children.Count;
        }
        else
        {
            CalculateRowSpacing(rowWidth, children, childSizes, out innerSpacing, out outerSpacing);
        }

        double x = hierarchical
            ? outerSpacing
            : -GetX(ScrollOffset) + outerSpacing;

        for (var i = 0; i < children.Count; i++)
        {
            IItemContainerInfo child = children[i];
            Size childSize = childSizes[i];
            child.Arrange(CreateRect(x, y, GetWidth(childSize) + extraWidth, GetHeight(childSize)));
            x += GetWidth(childSize) + extraWidth + innerSpacing;
        }
    }

    private void CalculateRowSpacing(double rowWidth,
                                     List<IItemContainerInfo> children,
                                     List<Size> childSizes,
                                     out double innerSpacing,
                                     out double outerSpacing)
    {
        int childCount;
        double summedUpChildWidth;

        if (AllowDifferentSizedItems)
        {
            childCount = children.Count;
            summedUpChildWidth = childSizes.Sum(GetWidth);
        }
        else
        {
            childCount = (int)Math.Max(1, Math.Floor(rowWidth / GetWidth(_sizeOfFirstItem!.Value)));
            summedUpChildWidth = childCount * GetWidth(_sizeOfFirstItem.Value);
        }

        double unusedWidth = Math.Max(0, rowWidth - summedUpChildWidth);

        switch (SpacingMode)
        {
            case SpacingMode.Uniform:
                innerSpacing = outerSpacing = unusedWidth / (childCount + 1);
                break;

            case SpacingMode.BetweenItemsOnly:
                innerSpacing = unusedWidth / Math.Max(childCount - 1, 1);
                outerSpacing = 0;
                break;

            case SpacingMode.StartAndEndOnly:
                innerSpacing = 0;
                outerSpacing = unusedWidth / 2;
                break;

            case SpacingMode.None:
            default:
                innerSpacing = 0;
                outerSpacing = 0;
                break;
        }
    }

    private Size CalculateAverageSize(ICollection<Size> sizes)
    {
        if (sizes.Any())
            return new Size(sizes.Average(size => size.Width), sizes.Average(size => size.Height));
        return Size.Empty;
    }

    #region scroll info

    // TODO determine line height

    protected override double GetLineUpScrollAmount() =>
        -Math.Min(GetAverageItemSize().Height * ScrollLineDeltaItem, ViewportSize.Height);

    protected override double GetLineDownScrollAmount() =>
        Math.Min(GetAverageItemSize().Height * ScrollLineDeltaItem, ViewportSize.Height);

    protected override double GetLineLeftScrollAmount() =>
        -Math.Min(GetAverageItemSize().Width * ScrollLineDeltaItem, ViewportSize.Width);

    protected override double GetLineRightScrollAmount() =>
        Math.Min(GetAverageItemSize().Width * ScrollLineDeltaItem, ViewportSize.Width);

    protected override double GetMouseWheelUpScrollAmount() =>
        -Math.Min(GetAverageItemSize().Height * MouseWheelDeltaItem, ViewportSize.Height);

    protected override double GetMouseWheelDownScrollAmount() =>
        Math.Min(GetAverageItemSize().Height * MouseWheelDeltaItem, ViewportSize.Height);

    protected override double GetMouseWheelLeftScrollAmount() =>
        -Math.Min(GetAverageItemSize().Width * MouseWheelDeltaItem, ViewportSize.Width);

    protected override double GetMouseWheelRightScrollAmount() =>
        Math.Min(GetAverageItemSize().Width * MouseWheelDeltaItem, ViewportSize.Width);

    protected override double GetPageUpScrollAmount() => -ViewportSize.Height;

    protected override double GetPageDownScrollAmount() => ViewportSize.Height;

    protected override double GetPageLeftScrollAmount() => -ViewportSize.Width;

    protected override double GetPageRightScrollAmount() => ViewportSize.Width;

    #endregion

    #region orientation aware helper methods

    protected double GetX(Point point) => Orientation == Orientation.Horizontal
        ? point.X
        : point.Y;

    protected double GetY(Point point) => Orientation == Orientation.Horizontal
        ? point.Y
        : point.X;

    protected double GetWidth(Size size) => Orientation == Orientation.Horizontal
        ? size.Width
        : size.Height;

    protected double GetHeight(Size size) => Orientation == Orientation.Horizontal
        ? size.Height
        : size.Width;

    protected Point CreatePoint(double x, double y) => Orientation == Orientation.Horizontal
        ? new Point(x, y)
        : new Point(y, x);

    protected Size CreateSize(double width, double height) => Orientation == Orientation.Horizontal
        ? new Size(width, height)
        : new Size(height, width);

    protected Rect CreateRect(double x, double y, double width, double height) => Orientation == Orientation.Horizontal
        ? new Rect(x, y, width, height)
        : new Rect(y, x, height, width);

    #endregion
}