using System;

namespace FEx.MVVM.Abstractions.Interfaces;

public interface ILink
{
    string PropertyName { get; }
    Guid Id { get; }
    object DefaultValue { get; }
    Type PropertyType { get; }
    void Unlink(bool resetPropertyValue);
    void UnlinkChildren(bool resetPropertyValue);
    void ResetProperty();
    object GetPropertyValue();
    void OnPropertyChange(object? oldValue, object? newValue);
    void Initialize();
    void AddChild(ILink link);
    void RelinkChildren<T>(T newSender, Action onNewSender) where T : ILinkableNotifyPropertyChanged;
}