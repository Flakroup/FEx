using FEx.MVVM.Abstractions.Interfaces;
using System;
using System.Threading.Tasks;

namespace FEx.MVVM.Commands;

public class AsyncDelegateCommand : AsyncDelegateCommandBase, IAsyncCommand
{
    private readonly Func<Task> _execute;

    /// <summary>
    ///     Create a new AsyncDelegateCommand
    /// </summary>
    /// <param name="execute">Function to execute</param>
    /// <param name="canExecute">Function to call to determine if it can be executed</param>
    /// <param name="onException">Action callback when an exception occurs</param>
    /// <param name="continueOnCapturedContext">If the context should be captured on exception</param>
    public AsyncDelegateCommand(Func<Task> execute,
                                Func<object, bool> canExecute = null,
                                Action<Exception> onException = null,
                                bool continueOnCapturedContext = false)
        : base(canExecute, onException, continueOnCapturedContext)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    /// <summary>
    ///     Execute the command async.
    /// </summary>
    /// <returns>Task of action being executed that can be awaited.</returns>
    public async Task ExecuteAsync() => await TryRunTaskAsync(_execute);

    public override void Execute(object parameter) => SafeFireAndForget(ExecuteAsync);
}

public class AsyncDelegateCommand<T> : AsyncDelegateCommandBase, IAsyncCommand<T> where T : class
{
    private readonly Func<T, Task> _execute;

    /// <summary>
    ///     Create a new AsyncDelegateCommand
    /// </summary>
    /// <param name="execute">Function to execute</param>
    /// <param name="canExecute">Function to call to determine if it can be executed</param>
    /// <param name="onException">Action callback when an exception occurs</param>
    /// <param name="continueOnCapturedContext">If the context should be captured on exception</param>
    public AsyncDelegateCommand(Func<T, Task> execute,
                                Func<object, bool> canExecute = null,
                                Action<Exception> onException = null,
                                bool continueOnCapturedContext = false)
        : base(canExecute, onException, continueOnCapturedContext)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    /// <summary>
    ///     Execute the command async.
    /// </summary>
    /// <returns>Task that is executing and can be awaited.</returns>
    public async Task ExecuteAsync(T parameter) => await TryRunTaskAsync(() => _execute(parameter));

    public override void Execute(object parameter)
    {
        if (IsValidCommandParameter<T>(parameter))
            SafeFireAndForget(() => ExecuteAsync((T)parameter));
    }
}