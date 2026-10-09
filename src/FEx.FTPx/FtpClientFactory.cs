using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions;
using FluentFTP;
using FluentFTP.Proxy.AsyncProxy;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.FTPx;

public class FtpClientFactory
{
    private readonly SemaphoreSlim _semaphore;
    public Uri HostUri { get; }
    public ICredentials ProxyCredentials { get; set; } = CredentialCache.DefaultCredentials;
    public string? ProxyHost { get; set; }
    public int ProxyPort { get; set; }

    public Task<AsyncFtpClient> CreateAsync(string? user = null, string? pass = null, bool useProxy = false, int port = 0)
    {
        var credentials = user != null || pass != null
            ? new NetworkCredential(user, pass)
            : null;

        var proxy = GetProxy(useProxy);

        return CreateAsync(credentials, proxy, port);
    }

    public Task<AsyncFtpClient> CreateAsync(string? user = null, string? pass = null, FtpProxyProfile? proxy = null, int port = 0)
    {
        var credentials = user != null || pass != null
            ? new NetworkCredential(user, pass)
            : null;

        return CreateAsync(credentials, proxy, port);
    }

    public Task<AsyncFtpClient> CreateAsync(NetworkCredential? credentials = null, bool useProxy = false, int port = 0)
    {
        var proxy = GetProxy(useProxy);

        return CreateAsync(credentials, proxy, port);
    }

    public async Task<AsyncFtpClient> CreateAsync(NetworkCredential? credentials = null, FtpProxyProfile? proxy = null, int port = 0)
    {
        await _semaphore.WaitAsync();

        try
        {
            AsyncFtpClient client;

            if (proxy != null)
                client = new AsyncFtpClientHttp11Proxy(proxy);
            else
                client = new();

            if (HostUri != null)
                client.Host = HostUri.AbsoluteUri;

            if (credentials != null)
                client.Credentials = credentials;

            if (port != 0)
                client.Port = port;

            return client;
        }
        catch
        {
            _semaphore.Release();

            throw;
        }
    }

    public async Task ReleaseClientAsync(AsyncFtpClient client)
    {
        if (!client.IsDisposed)
        {
            if (client.IsConnected)
                await client.Disconnect();

#pragma warning disable IDISP007 // factory release pattern, client created by CreateAsync
            await client.DisposeAsync();
#pragma warning restore IDISP007
        }

        _semaphore.Release();
    }

    protected void OnValidateCertificate(FtpSslValidationEventArgs e)
    {
        e.Accept = true;
    }

    internal FtpProxyProfile? GetProxy(bool useProxy)
    {
        if (useProxy)
            return new()
            {
                ProxyCredentials = (NetworkCredential)ProxyCredentials,
                ProxyHost = ProxyHost,
                ProxyPort = ProxyPort
            };

        return null;
    }

    #region Singleton
    private static ISynchronizedAccessService LockSrv => FExCoreStatics.SynchronizedAccessService;
    private static ConcurrentDictionary<string, FtpClientFactory> Instances { get; } = new();

    public static async Task<FtpClientFactory> GetInstanceAsync(string hostUri, int maxParallel = 5)
    {
#pragma warning disable IDISP001 // semaphore from LockSrv, lifetime managed by lock service
        var semaphore = LockSrv.EnsureLock($"{hostUri}@{nameof(FtpClientFactory)}_Instance");
#pragma warning restore IDISP001
        await semaphore.WaitAsync();
        var res = Instances.GetOrAdd(hostUri, _ => new(hostUri, maxParallel));
        semaphore.Release();

        return res;
    }

    private FtpClientFactory(string hostUri, int maxParallel)
    {
        HostUri = new(hostUri);
        var semaphoreKey = $"{HostUri.AbsoluteUri}@{nameof(FtpClientFactory)}";
        _semaphore = LockSrv.EnsureLock(semaphoreKey, maxParallel);
    }
    #endregion
}