using System;
using System.Windows.Input;

namespace FEx.WPFx.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class HotkeyActionAttribute : Attribute
{
    public Key Key { get; }

    public ModifierKeys Modifiers { get; }

    public HotkeyActionAttribute(Key key)
    {
        Key = key;
        Modifiers = ModifierKeys.None;
    }

    public HotkeyActionAttribute(Key key, ModifierKeys modifiers)
    {
        Key = key;
        Modifiers = modifiers;
    }
}