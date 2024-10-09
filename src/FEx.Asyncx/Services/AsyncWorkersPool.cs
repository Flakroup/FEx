using FEx.Asyncx.Abstractions;
using FEx.Asyncx.Helpers;
using FEx.Basics.Collections.Concurrent;
using FEx.Basics.Extensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Asyncx.Services;

/// <summary>
/// </summary>
/// <typeparam name="TWorker"></typeparam>
/// <typeparam name="TQueue"></typeparam>
/// <remarks>Doesn't require <c>BeginInitialization();</c> call in .ctor</remarks>
public abstract class AsyncWorkersPool<TWorker, TQueue> : AsyncInitializable
    where TWorker : AsyncWorker<TWorker, TQueue>
{
    public TWorker IdleWorker { get; protected set; }

    protected AsyncQueue<string, TQueue> Queue { get; }
    protected ConcurrentList<TWorker> Workers { get; }
    private SemaphoreSlim Semaphore { get; }

    protected AsyncWorkersPool(int poolSize)
    {
        Queue = new(ex => ex.HandleException(), poolSize);
        Workers = [];
        Semaphore = new(1, 1);

        BeginInitialization();
    }

    public async Task<TQueue> ExecuteOnPoolAsync(Func<TWorker, string, Task<TQueue>> func,
                                                 Func<Guid, string> getId = null)
    {
        var guid = Guid.NewGuid();
        string id = getId?.Invoke(guid) ?? guid.ToString();

        return await Queue.GetOrAddAsync(id, () => ExecuteAsync(w => func(w, id)));
    }

    public async Task WaitForAllClientsAsync() => await Task.WhenAll(Workers.Select(x => x.CurrentTask));

    protected abstract TWorker GetNewWorker(int id);

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();
        InitializePool(Queue.Limit);
        await Task.WhenAll(Workers.Select(x => x.InitializeAsync()).ToArray());
    }

    protected void InitializePool(int poolSize)
    {
        Workers.Clear();
        Workers.AddRange(Enumerable.Range(0, poolSize).Select(GetNewWorker));
        IdleWorker = GetNewWorker(0);
    }

    private async Task<TQueue> ExecuteAsync(Func<TWorker, Task<TQueue>> func)
    {
        Task<TQueue> task;
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