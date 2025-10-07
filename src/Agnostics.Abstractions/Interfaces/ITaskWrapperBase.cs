using FEx.Abstractions.Flow.Errors;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FEx.Abstractions.Interfaces;

public interface ITaskWrapperBase<TTask, out TResult> : ITaskWrapperBase
    where TTask : Task where TResult : class, IResult<ExceptionError>
{
    TResult Result { get; }
    TTask Task { get; }
    void SetTask(Func<TTask> task);
}

public interface ITaskWrapperBase
{
    Guid Id { get; }
    bool IsFinished { get; }
    StackTrace TaskCreationStackTrace { get; }

    void SetException(Exception exception);
}