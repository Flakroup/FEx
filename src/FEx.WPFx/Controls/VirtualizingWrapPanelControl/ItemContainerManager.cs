using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FEx.WPFx.Controls.VirtualizingWrapPanelControl;

internal class ItemContainerManagerItemsChangedEventArgs
{
    public NotifyCollectionChangedAction Action { get; }
    public IReadOnlyCollection<IItemContainerInfo> RemovedContainers { get; }

    public ItemContainerManagerItemsChangedEventArgs(NotifyCollectionChangedAction action,
                                                     IReadOnlyCollection<IItemContainerInfo> removedContainers)
    {
        Action = action;
        RemovedContainers = removedContainers;
    }
}

internal interface IItemContainerManager
{
    public event EventHandler<ItemContainerManagerItemsChangedEventArgs> ItemsChanged;

    bool IsRecycling { get; set; }

    ReadOnlyCollection<object> Items { get; }

#if NET6_0_OR_GREATER
    IReadOnlySet<IItemContainerInfo> RealizedContainers { get; }
#else
    IEnumerable<IItemContainerInfo> RealizedContainers { get; }
#endif

#if NET6_0_OR_GREATER
    IReadOnlySet<IItemContainerInfo> CachedContainers { get; }
#else
    IEnumerable<IItemContainerInfo> CachedContainers { get; }
#endif

    /// <summary>
    ///     Realizes the specified item. If the item is already realized, nothing happens.
    /// </summary>
    /// <param name="itemIndex">Index of the item to relaize</param>
    /// <param name="isNewlyRealized">Indicates whether the specified item is newly realized</param>
    /// <param name="isNewContainer">Indicates whether a new container was generated</param>
    /// <returns>A object with information about the container of the specified item</returns>
    IItemContainerInfo Realize(int itemIndex, out bool isNewlyRealized, out bool isNewContainer);

    /// <returns>true if the container should be removed, otherwise false (container is recylced)</returns>
    bool Virtualize(IItemContainerInfo containerInfo);

    int FindItemIndexOfContainer(IItemContainerInfo containerInfo);
}

internal class ItemContainerManager : IItemContainerManager
{
    public event EventHandler<ItemContainerManagerItemsChangedEventArgs> ItemsChanged;

    public bool IsRecycling { get; set; }

    public ReadOnlyCollection<object> Items => _itemContainerGenerator.Items;

#if NET6_0_OR_GREATER
    public IReadOnlySet<IItemContainerInfo> RealizedContainers => _realizedContainers;
#else
    public IEnumerable<IItemContainerInfo> RealizedContainers => _realizedContainers;
#endif

#if NET6_0_OR_GREATER
    public IReadOnlySet<IItemContainerInfo> CachedContainers => _cachedContainers;
#else
    public IEnumerable<IItemContainerInfo> CachedContainers => _cachedContainers;
#endif

    private readonly HashSet<IItemContainerInfo> _realizedContainers = [];

    private readonly HashSet<IItemContainerInfo> _cachedContainers = [];

    private readonly ItemContainerGenerator _itemContainerGenerator;

    private readonly IRecyclingItemContainerGenerator _recyclingItemContainerGenerator;

    public ItemContainerManager(ItemContainerGenerator itemContainerGenerator)
    {
        _itemContainerGenerator = itemContainerGenerator;
        _recyclingItemContainerGenerator = itemContainerGenerator;
        itemContainerGenerator.ItemsChanged += ItemContainerGenerator_ItemsChanged;
    }

    private void ItemContainerGenerator_ItemsChanged(object sender, ItemsChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _realizedContainers.Clear();
            _cachedContainers.Clear();
        }

        if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace)
        {
            var removedCotainers = _realizedContainers.Where(container => !Items.Contains(container.Item)).ToList();
            removedCotainers.ForEach(container => _realizedContainers.Remove(container));

            if (IsRecycling)
                removedCotainers.ForEach(container => _cachedContainers.Add(container));

            ItemsChanged?.Invoke(this, new(e.Action, removedCotainers));
        }
        else
        {
            ItemsChanged?.Invoke(this, new(e.Action, []));
        }
    }

    public IItemContainerInfo Realize(int itemIndex, out bool isNewlyRealized, out bool isNewContainer)
    {
        object item = Items[itemIndex];

        if (RealizedContainers.FirstOrDefault(container => container.Item == item) is { } containerInfo)
        {
            isNewlyRealized = false;
            isNewContainer = false;

            return containerInfo;
        }

        isNewlyRealized = true;
        GeneratorPosition generatorPosition = _recyclingItemContainerGenerator.GeneratorPositionFromIndex(itemIndex);

        using (_recyclingItemContainerGenerator.StartAt(generatorPosition, GeneratorDirection.Forward))
        {
            DependencyObject container = _recyclingItemContainerGenerator.GenerateNext(out isNewContainer);
            _recyclingItemContainerGenerator.PrepareItemContainer(container);
            containerInfo = ItemContainerInfo.For((UIElement)container, item);
            _cachedContainers.Remove(containerInfo);
            _realizedContainers.Add(containerInfo);

            return containerInfo;
        }
    }

    public bool Virtualize(IItemContainerInfo containerInfo)
    {
        int itemIndex = FindItemIndexOfContainer(containerInfo);

        if (itemIndex == -1)
        {
            Debug.WriteLine("Virtualize no more existing item");
            _realizedContainers.Remove(containerInfo);

            return true;
        }

        GeneratorPosition generatorPosition = _recyclingItemContainerGenerator.GeneratorPositionFromIndex(itemIndex);

        if (IsRecycling)
        {
            _recyclingItemContainerGenerator.Recycle(generatorPosition, 1);
            _realizedContainers.Remove(containerInfo);
            _cachedContainers.Add(containerInfo);

            return false;
        }

        _recyclingItemContainerGenerator.Remove(generatorPosition, 1);
        _realizedContainers.Remove(containerInfo);

        return true;
    }

    public int FindItemIndexOfContainer(IItemContainerInfo containerInfo) =>
        _itemContainerGenerator.IndexFromContainer(containerInfo.UIElement);
}