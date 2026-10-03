using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces.Flow;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>Wraps a task together with its outcome expressed as a result object</summary>
/// <typeparam name="TTask">The wrapped task type.</typeparam>
/// <typeparam name="TResult">The result type describing success or the captured exception.</typeparam>
public interface ITaskWrapperBase<TTask, out TResult> : ITaskWrapperBase
    where TTask : Task where TResult : class, IResult<ExceptionError>
{
    /// <summary>Gets the outcome of the wrapped task.</summary>
    TResult Result { get; }
    /// <summary>Gets the wrapped task.</summary>
    TTask Task { get; }
    /// <summary>Sets the wrapped task from a factory</summary>
    /// <param name="task">The factory that creates the task.</param>
    void SetTask(Func<TTask> task);
}

/// <summary>Non-generic part of a task wrapper that tracks identity, completion and creation origin.</summary>
public interface ITaskWrapperBase
{
    /// <summary>Gets the unique identifier of the wrapper.</summary>
    Guid Id { get; }
    /// <summary>Gets a value indicating whether the wrapped task has finished.</summary>
    bool IsFinished { get; }
    /// <summary>Gets the stack trace captured when the task was created.</summary>
    StackTrace TaskCreationStackTrace { get; }

    /// <summary>Marks the wrapper as failed with the given exception</summary>
    /// <param name="exception">The exception to record.</param>
    void SetException(Exception exception);
}