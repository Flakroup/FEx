using System;

namespace FEx.Basics.Flow;

public interface IExceptionError : IStackError
{
    Exception Exception { get; }
}