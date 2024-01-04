using FEx.Abstractions;
using System;
using System.Diagnostics;

namespace FEx.Basics.Helpers;

public abstract class TaskWrapperBase : ITaskWrapper
{
    public Guid Id { get; }

    public bool IsFinished { get; protected set; }

    public StackTrace TaskCreationStackTrace { get; }

    protected TaskWrapperBase(bool setStackTrace = false)
    {
        Id = Guid.NewGuid();

        TaskCreationStackTrace = setStackTrace
            ? FExBasics.StackTraceProvider.GetStackTrace()
            : null;
    }

    public abstract void SetException(Exception exception);
}