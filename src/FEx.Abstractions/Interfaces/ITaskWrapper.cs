using System;
using System.Diagnostics;

namespace FEx.Abstractions.Interfaces;

public interface ITaskWrapper
{
    Guid Id { get; }
    bool IsFinished { get; }
    StackTrace TaskCreationStackTrace { get; }
    void SetException(Exception exception);
}