using FEx.Basics.Extensions;
using FEx.Extensions;
using MvvmHelpers;
using MvvmHelpers.Exceptions;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FEx.MVVM.Commands;

public abstract class AsyncDelegateCommandBase : ICommand, INotifyPropertyChanged
{
    private const int MaxThreadCount = 1;
    private readonly Func<object, bool> _canExecute;
    private readonly bool _continueOnCapturedContext;
    private readonly Action<Exception> _onException;
    private readonly SemaphoreSlim _semaphore;
    private readonly SynchronizationContext _context;
    private readonly WeakEventManager _weakEventManager;

    /// <summary>
    ///     Event triggered when Can Excecute changes.
    /// </summary>
    public event EventHandler CanExecuteChanged
    {
        add => _weakEventManager.AddEventHandler(value);
        remove => _weakEventManager.RemoveEventHandler(value);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public bool IsExecuting => _semaphore.CurrentCount == 0;

    protected AsyncDelegateCommandBase(Func<object, bool> canExecute = null,
                                       Action<Exception> onException = null,
                                       bool continueOnCapturedContext = false)
    {
        _canExecute = canExecute;
        _onException = onException;
        _continueOnCapturedContext = continueOnCapturedContext;
        _weakEventManager = new();
        _semaphore = new(MaxThreadCount, MaxThreadCount);
        _context = SynchronizationContext.Current;
    }

    /// <summary>
    ///     Invoke the CanExecute method and return if it can be executed.
    /// </summary>
    /// <param name="parameter">Parameter to pass to CanExecute.</param>
    /// <returns>If it can be executed.</returns>
    public bool CanExecute(object parameter) => !IsExecuting && (_canExecute is null || _canExecute(parameter));

    public abstract void Execute(object parameter);

    /// <summary>
    ///     Raise a CanExecute change event.
    /// </summary>
    public void RaiseCanExecuteChanged() => _context.Post(_ => { _weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(CanExecuteChanged)); }, null);

    /// <summary>
    ///     Determines whether [is valid command parameter] [the specified o].
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="o">The o.</param>
    /// <returns>
    ///     <c>true</c> if [is valid command parameter] [the specified o]; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="InvalidCommandParameterException">
    /// </exception>
    protected static bool IsValidCommandParameter<T>(object o) where T : class
    {
        Type t = typeof(T);

        if (o is not null)
        {
            // The parameter isn't null, so we don't have to worry whether null is a valid option
            var p = o as T;

            return p is not null
                ? true
                : throw new InvalidCommandParameterException(t, o.GetType());
        }

        // The parameter is null. Is T Nullable?
        if (Nullable.GetUnderlyingType(t) is not null)
            return true;

        // Not a Nullable, if it's a value type then null is not valid
        return t.GetTypeInfo().IsValueType
            ? throw new InvalidCommandParameterException(t)
            : true;
    }

    [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods")]
    protected async void SafeFireAndForget(Func<Task> task)
    {
        try
        {
            await task();
        }
        catch (Exception ex)
        {
            if (_onException is not null)
                _onException(ex);
            else
                ex.HandleException();
        }
    }

    protected async Task TryRunTaskAsync(Func<Task> task)
    {
        if (await _semaphore.WaitAsync(0))
            try
            {
                NotifyProgress();
                await task().ConfigureAwait(_continueOnCapturedContext);
            }
            finally
            {
                _semaphore.Release();
                NotifyProgress();
            }
    }

    private void NotifyProgress()
    {
        RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(IsExecuting));
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (propertyName is null
            || PropertyChanged is null)
            return;

        PropertyChanged.HandlePropertyChanged(this, propertyName);
    }
}