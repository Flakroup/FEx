using System;

namespace FEx.MVVM.Abstractions.Interfaces;

public interface IViewModelBase : IEquatable<IViewModelBase>
{
    string Id { get; }
    bool IsInDesignMode { get; }
    string Title { get; set; }
}