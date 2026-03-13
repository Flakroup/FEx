using System;

namespace FEx.MVVM.Rx.Abstractions.Interfaces;

/// <summary>
/// An <see cref="IProgress{T}" /> that is disposable.
/// </summary>
/// <typeparam name="T">The type of progress updates.</typeparam>
/// <example>https://gist.github.com/StephenCleary/4248e50b4cb52b933c0d</example>
public interface IDisposableProgress<in T> : IProgress<T>, IDisposableProgress
{
}

public interface IDisposableProgress : IDisposable
{
    bool IsDisposed { get; }
}