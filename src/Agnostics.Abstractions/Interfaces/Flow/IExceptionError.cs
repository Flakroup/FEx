using System;

namespace FEx.Agnostics.Abstractions.Interfaces.Flow;

public interface IExceptionError : IStackError
{
    Exception? Exception { get; }
}