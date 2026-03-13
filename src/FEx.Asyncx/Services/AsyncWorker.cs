using FEx.Agnostics.Abstractions.Utilities;
using FEx.Asyncx.Abstractions;
using System;
using System.Threading.Tasks;

namespace FEx.Asyncx.Services;

/// <summary>
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TResult"></typeparam>
/// <remarks>Doesn't require <c>BeginInitialization();</c> call in .ctor</remarks>
public abstract class AsyncWorker<T, TResult> : AsyncInitializable where T : class
{
    private bool _isBusy;

    public int Id { get; }
    public Task<T> CurrentTask => Tcs.Task;
    public Task<bool> IdleTask => IdleTcs.Task;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (value && _isBusy)
                throw new InvalidOperationException("This worker is already busy");

            EqualityHelper.SetFieldIfChanged(ref _isBusy, value);
        }
    }

    private TaskCompletionSource<T> Tcs { get; set; }
    private TaskCompletionSource<bool> IdleTcs { get; set; }

    protected AsyncWorker(int id)
    {
        Id = id;

        BeginInitialization();
    }

    public async Task<TResult> ExecuteTaskAsync(Func<T, Task<TResult>> func)
    {
        StartCurrentTask();

        return await ExecuteAsync(func);
    }

    protected virtual void EndCurrentTask()
    {
        IdleTcs = new();
        IsBusy = false;
        Tcs.SetResult(this as T);
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        StartCurrentTask();
        EndCurrentTask();
    }

    protected async Task<TResult> ExecuteAsync(Func<T, Task<TResult>> func)
    {
        try
        {
            return await func(this as T);
        }
        finally
        {
            EndCurrentTask();
        }
    }

    private void StartCurrentTask()
    {
        Tcs = new();
        IdleTcs?.SetResult(false);
        IsBusy = true;
    }
}