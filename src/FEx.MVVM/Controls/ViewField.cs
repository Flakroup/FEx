using FEx.Agnostics.BaseObjects;
using System;

namespace FEx.MVVM.Controls;

public class ViewField : NotifyPropertyChanged
{
    private bool _isVisible;
    public string? Header { get; set; }
    public string? ToolTip { get; set; }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (SetProperty(ref _isVisible, value)
                && IsVisiblePersistence != null)
                IsVisiblePersistence(_isVisible);
        }
    }

    public Action<bool>? IsVisiblePersistence { get; set; }

    public ViewField()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewField" /> class.
    /// </summary>
    /// <param name="header">The header.</param>
    /// <param name="toolTip">The tool tip.</param>
    /// <param name="isVisible">if set to <c>true</c> [is visible].</param>
    /// <param name="isVisiblePersistence">The is visible persistence.</param>
    public ViewField(string header, string toolTip, bool isVisible, Action<bool> isVisiblePersistence)
    {
        Header = header;
        ToolTip = toolTip;
        IsVisible = isVisible;
        IsVisiblePersistence = isVisiblePersistence;
    }
}