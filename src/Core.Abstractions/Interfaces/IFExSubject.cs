using System;

namespace FEx.Rx.Abstractions.Interfaces;

public interface IFExSubject<T> : IDisposable, IObservable<T>
{
    void OnNext(T value);
}