using System;

namespace FEx.Core.Abstractions.Interfaces;

public interface IFExSubject<T> : IDisposable, IObservable<T>
{
    void OnNext(T value);
    void OnCompleted();
}