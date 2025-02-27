using FEx.Asyncx.Abstractions;
using FEx.Asyncx.Helpers;
using FEx.Basics.Collections.Concurrent;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Asyncx.Services;

/// <summary>
/// </summary>
/// <typeparam name="TWorker"></typeparam>
/// <remarks>Doesn't require <c>BeginInitialization();</c> call in .ctor</remarks>
public abstract class AsyncWorkersPool<TWorker, TResult> : AsyncInitializable
    where TWorker : AsyncWorker<TWorker, TResult>
{
    public TWorker IdleWorker { get; protected set; }

    protected AsyncProcessingQueue ProcessingQueue { get; }
    protected ConcurrentList<TWorker> Workers { get; }
    private SemaphoreSlim Semaphore { get; }

    protected AsyncWorkersPool(uint poolSize)
    {
        ProcessingQueue = new(poolSize);
        Workers = [];
        Semaphore = new(1, 1);

        BeginInitialization();
    }

    public async Task<TResult> ExecuteOnPoolAsync(Func<TWorker, string, Task<TResult>> func,
                                                       Func<Guid, string> getId = null)
    {
        var guid = Guid.NewGuid();
        string id = getId?.Invoke(guid) ?? guid.ToString();

        return await ProcessingQueue.EnqueueAsync(() => ExecuteAsync(w => func(w, id)));
    }

    public async Task WaitForAllClientsAsync() => await Task.WhenAll(Workers.Select(x => x.CurrentTask));

    protected abstract TWorker GetNewWorker(int id);

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();
        InitializePool((int)ProcessingQueue.ConcurrencyLimit);
        await Task.WhenAll([.. Workers.Select(x => x.InitializeAsync())]);
    }

    protected void InitializePool(int poolSize)
    {
        Workers.Clear();
        Workers.AddRange(Enumerable.Range(0, poolSize).Select(GetNewWorker));
        IdleWorker = GetNewWorker(0);
    }

    private async Task<TResult> ExecuteAsync(Func<TWorker, Task<TResult>> func)
    {
        Task<TResult> task;
        await Semaphore.WaitAsync();

        try
        {
            TWorker worker = Workers.FirstOrDefault(x => !x.IsBusy);

            if (worker is null)
            {
                var currentTask = await Task.WhenAny(Workers.Select(x => x.CurrentTask).ToArray());
                worker = await currentTask;
            }

            await worker.InitializeAsync();

            task = worker.ExecuteTaskAsync(func);
        }
        finally
        {
            Semaphore.Release();
        }

        return await task;
    }
}