using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Models;
using FEx.Agnostics.Collections.Concurrent;
using FEx.Core.Abstractions;
using FEx.Downloader.Clients;
using FEx.Downloader.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Downloader.Services;

public class HttpClientService : IDisposable
{
    private readonly SemaphoreSlim _clientSemaphore;

    private bool _isDisposed;

    public string Host { get; }

    public WebRequestParams? DefaultPars { get; }
    private static ISynchronizedAccessService LockSrv => FExCoreStatics.SynchronizedAccessService;

    private static ConcurrentDictionary<string, HttpClientService> Instances { get; } = new();

    private ConcurrentList<FlakHttpClient> Clients { get; }

    private HttpClientService(string host, WebRequestParams? pars = null, int clientsCount = 2)
    {
        Host = host;
        _clientSemaphore = new(1, 1);
        Clients = [];
        DefaultPars = pars;
        PrepareClients(clientsCount);
    }

    public static async Task<HttpClientService> GetInstanceAsync(string urlHost,
                                                                 WebRequestParams? pars = null,
                                                                 int clientsCount = 2) =>
        await GetInstanceAsync(new(urlHost, pars, clientsCount));

    public static async Task<HttpClientService> GetInstanceAsync(HttpClientServiceStub stub)
    {
        if (!Instances.TryGetValue(stub.Host, out var res)
            || res is null)
        {
            await PrepareInstanceAsync(stub);
            res = Instances[stub.Host];
        }

        return res;
    }

    public static async Task PrepareInstanceAsync(string urlHost, WebRequestParams? pars = null, int clientsCount = 2) =>
        await PrepareInstanceAsync(new(urlHost, pars, clientsCount));

    public static async Task PrepareInstanceAsync(HttpClientServiceStub stub)
    {
        if (!Instances.ContainsKey(stub.Host)
            || Instances[stub.Host] is null)
        {
#pragma warning disable IDISP001 // semaphore from LockSrv, lifetime managed by lock service
            var loadingSemaphore = LockSrv.EnsureLock($"{nameof(HttpClientService)}@{stub.Host}");
#pragma warning restore IDISP001
            await loadingSemaphore.WaitAsync();

            try
            {
                if (!Instances.ContainsKey(stub.Host))
                    Instances.TryAdd(stub.Host, new(stub.Host, stub.Pars, stub.HttpClientsCount));
                else if (Instances[stub.Host] is null)
                    Instances[stub.Host] = new(stub.Host, stub.Pars, stub.HttpClientsCount);
            }
            finally
            {
                loadingSemaphore.Release();
            }
        }
    }

    public static async Task RemoveInstanceAsync(string urlHost, bool waitForBusyClients = true)
    {
        if (Instances.TryGetValue(urlHost, out var instance))
        {
#pragma warning disable IDISP001 // semaphore from LockSrv, lifetime managed by lock service
            var loadingSemaphore = LockSrv.EnsureLock($"{nameof(HttpClientService)}@{urlHost}");
#pragma warning restore IDISP001
            await loadingSemaphore.WaitAsync();

            try
            {
                if (waitForBusyClients)
                    await AsyncStatics.DelayUntilAsync(() => instance.Clients.Any(x => x.IsBusy));

                Instances.TryRemove(urlHost, out var srv);
                srv?.Dispose();
            }
            finally
            {
                loadingSemaphore.Release();
            }

            LockSrv.RemoveLock($"{nameof(HttpClientService)}@{urlHost}");
        }
    }

    public async Task DoHttpClientActionAsync(Action<FlakHttpClient> action, WebRequestParams? pars = null)
    {
        var client = await GetClientAsync(pars);

        try
        {
            action(client);
        }
        finally
        {
            await client.DelayAsync();
            client.Idle();
        }
    }

    public async Task DoHttpClientActionAsync(Func<FlakHttpClient, Task> func, WebRequestParams? pars = null)
    {
        var client = await GetClientAsync(pars);

        try
        {
            await func(client);
        }
        finally
        {
            await client.DelayAsync();
            client.Idle();
        }
    }

    public async Task<T> DoHttpClientActionAsync<T>(Func<FlakHttpClient, Task<T>> func, WebRequestParams? pars = null)
    {
        var client = await GetClientAsync(pars);

        try
        {
            return await func(client);
        }
        finally
        {
            await client.DelayAsync();
            client.Idle();
        }
    }

    private void PrepareClients(int clientsCount)
    {
        Clients.Clear();
        Clients.AddRange(Enumerable.Range(0, clientsCount).Select(_ => new FlakHttpClient(DefaultPars ?? new(), false)));
    }

    private async Task<FlakHttpClient> GetClientAsync(WebRequestParams? pars = null)
    {
        await _clientSemaphore.WaitAsync();

        try
        {
            await AsyncStatics.DelayUntilAsync(() => Clients.All(x => x.IsBusy));

            var client = Clients.First(x => !x.IsBusy);
            client.Busy();

            if (pars is not null)
                client.Pars = pars;
            else if (DefaultPars is not null)
                client.Pars = DefaultPars;

            return client;
        }
        finally
        {
            _clientSemaphore.Release();
        }
    }

    ~HttpClientService()
    {
        Dispose(false);
    }

    #region IDisposable
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            _clientSemaphore?.Dispose();
            Parallel.ForEach(Clients, c => c?.Dispose());
            Clients.Clear();
        }

        _isDisposed = true;
    }
    #endregion
}