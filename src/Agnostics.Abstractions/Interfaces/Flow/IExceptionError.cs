using System;

namespace FEx.Agnostics.Abstractions.Interfaces.Flow;

/// <summary>An error that wraps the exception that caused it.</summary>
public interface IExceptionError : IStackError
{
    /// <summary>Gets the wrapped exception.</summary>
    Exception? Exception { get; }
}