using FEx.Asyncx.Abstractions;
using FEx.Asyncx.Interfaces;

namespace FEx.Asyncx.Services;

/// <summary>
/// </summary>
/// <typeparam name="TPool"></typeparam>
/// <typeparam name="TWorker"></typeparam>
/// <typeparam name="TQueue"></typeparam>
/// <typeparam name="TConf"></typeparam>
/// <remarks>Doesn't require <c>BeginInitialization();</c> call in .ctor</remarks>
public abstract class AsyncWorkersService<TPool, TWorker, TQueue, TConf> : AsyncInitializable
    where TPool : AsyncWorkersPool<TWorker, TQueue>
    where TWorker : AsyncWorker<TWorker, TQueue>
    where TConf : IAsyncWorkerConfig
{
    protected TPool Pool { get; set; }
    protected TConf Config { get; }

    protected AsyncWorkersService(uint poolSize, TConf config = default)
    {
        Config = config;
        OnConstruction();
        Pool = GetNewPool(poolSize);
        AddDependencies(Pool);

        BeginInitialization();
    }

    protected abstract TPool GetNewPool(uint poolSize);

    protected virtual void OnConstruction()
    {
    }
}