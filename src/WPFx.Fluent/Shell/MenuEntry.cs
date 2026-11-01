using System.ComponentModel;

namespace FEx.WPFx.Fluent.Shell;

/// <summary>
/// A menu row: the module plus whether it is the open one. Kept apart from the list's own selection, which is the
/// keyboard cursor - arrow keys move it, Enter opens - so highlighting the open module cannot ride on it.
/// </summary>
public sealed class MenuEntry(ShellModule module) : INotifyPropertyChanged
{
    private bool _isActive;

    public ShellModule Module { get; } = module;

    public string GroupLabel => Module.GroupLabel;

    public string Label => Module.Label;

    public string DisplayGlyph => Module.DisplayGlyph;

    public string Tooltip => Module.Tooltip;

    public bool IsAvailable => Module.IsAvailable;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value)
                return;

            _isActive = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
