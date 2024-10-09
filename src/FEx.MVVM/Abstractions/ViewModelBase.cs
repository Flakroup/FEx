using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Rx.BaseObjects;
using System;

namespace FEx.MVVM.Abstractions;

public abstract class ViewModelBase : LinkableReactiveNotifyPropertyChanged, IViewModelBase, IEquatable<ViewModelBase>
{
    private string _title;

    public string Id { get; }
    public bool IsInDesignMode { get; protected set; }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    protected ViewModelBase()
    {
        Id = Guid.NewGuid().ToString();
        IsInDesignMode = GetIsInDesignMode();
    }

    public bool Equals(IViewModelBase other)
    {
        if (other is null)
            return false;

        return ReferenceEquals(this, other) || Id == other.Id;
    }

    public bool Equals(ViewModelBase other) => Equals(other as IViewModelBase);

    public static bool operator ==(ViewModelBase left, ViewModelBase right) => Equals(left, right);

    public static bool operator !=(ViewModelBase left, ViewModelBase right) => !Equals(left, right);

    public override bool Equals(object obj) =>
        ReferenceEquals(this, obj) || obj is ViewModelBase other && Equals(other);

    public override int GetHashCode() =>
        Id is not null
            ? Id.GetHashCode()
            : 0;

    protected virtual bool GetIsInDesignMode() => false;
}