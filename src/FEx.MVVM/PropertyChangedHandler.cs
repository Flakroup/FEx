using System;
using System.ComponentModel;

namespace FEx.MVVM;

public class PropertyChangedHandler<T> where T : class, INotifyPropertyChanged
{
    private readonly Action<(T value, string propertyName)> _onPropertyChanged;
    private readonly Action<(T newSender, T oldSender)> _onSenderChange;
    private T _sender;

    public PropertyChangedHandler(Action<(T value, string propertyName)> onPropertyChanged,
                                  T sender = null,
                                  Action<(T newSender, T oldSender)> onSenderChange = null)
    {
        _onPropertyChanged = onPropertyChanged;
        _onSenderChange = onSenderChange;
        ChangeSender(sender);
    }

    public void ChangeSender(T sender)
    {
        T oldSender = _sender;

        if (_sender is not null)
            _sender.PropertyChanged -= SenderOnPropertyChanged;

        _sender = sender;

        if (_sender is not null)
            _sender.PropertyChanged += SenderOnPropertyChanged;

        _onSenderChange?.Invoke((_sender, oldSender));
    }

    private void SenderOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is not T source)
            return;

        _onPropertyChanged((source, e.PropertyName));
    }
}