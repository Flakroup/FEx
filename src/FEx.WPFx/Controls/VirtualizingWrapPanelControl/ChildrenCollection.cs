using System;
using System.Windows;

namespace FEx.WPFx.Controls.VirtualizingWrapPanelControl;

internal interface IChildrenCollection
{
    void AddChild(IItemContainerInfo child);
    void RemoveChild(IItemContainerInfo child);
}

internal class VirtualizingPanelWrapper : IChildrenCollection
{
    private readonly Action<UIElement> _addInternalChild;
    private readonly Action<UIElement> _removetInternalChild;

    public VirtualizingPanelWrapper(Action<UIElement> addInternalChild, Action<UIElement> removetInternalChild)
    {
        _addInternalChild = addInternalChild;
        _removetInternalChild = removetInternalChild;
    }

    public void AddChild(IItemContainerInfo containerInfo) => _addInternalChild.Invoke(containerInfo.UIElement);

    public void RemoveChild(IItemContainerInfo containerInfo) => _removetInternalChild.Invoke(containerInfo.UIElement);
}