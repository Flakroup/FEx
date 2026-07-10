using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Interfaces.Flow;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FEx.Core.Abstractions.Utilities;

public abstract class TaskWrapperBase<TTask, TResult> : ITaskWrapperBase<TTask, TResult> where TTask : Task
    where TResult : class, IResult<ExceptionError>
{
    // Invariant: _task is assigned via SetTask and _result on completion, before their getters are read.
    private TTask _task = null!;
    private TResult _result = null!;

    public Guid Id { get; }

    public bool IsFinished { get; protected set; }

    public StackTrace TaskCreationStackTrace { get; }

    public TResult Result
    {
        get => _result;
        protected set
        {
            _result = value;
            IsFinished = true;
        }
    }

    public TTask Task
    {
        get => _task;
        private set
        {
            if (_task is not null)
                throw new InvalidOperationException($"{nameof(Task)} is already set");

            _task = value;
        }
    }

    protected TaskWrapperBase(Func<TTask>? task = null, bool setStackTrace = true)
    {
        Id = Guid.NewGuid();

        if (task is not null)
            SetTask(task);

        // TaskCreationStackTrace is non-null-annotated (L0 interface) but is intentionally null
        // when stack-trace capture is disabled; null! preserves that behavior.
        TaskCreationStackTrace = setStackTrace
            ? FExCoreStatics.StackTraceProvider.GetStackTrace()
            : null!;
    }

    public void SetException(Exception exception) => Result = ConvertExceptionToError(exception);

    public void SetTask(Func<TTask> task) => Task = ExecuteTaskAsync(task);

    protected abstract TResult ConvertExceptionToError(Exception exception);

#pragma warning disable VSTHRD200
    protected abstract TTask ExecuteTaskAsync(Func<TTask> task);
#pragma warning restore VSTHRD200
}