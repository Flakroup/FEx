using System;

namespace FEx.Abstractions.Interfaces;

public interface IExceptionError : IStackError
{
    Exception Exception { get; }
}