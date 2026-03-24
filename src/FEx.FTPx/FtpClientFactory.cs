using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions;
using FluentFTP;
using FluentFTP.Helpers;
using FluentFTP.Proxy;
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
    public string ProxyHost { get; set; }
    public int ProxyPort { get; set; }

    public Task<FtpClient> CreateAsync(string user = null, string pass = null, bool useProxy = false, int port = 0)
    {
        var credentials = user != null || pass != null
            ? new NetworkCredential(user, pass)
            : null;

        var proxy = GetProxy(useProxy);

        return CreateAsync(credentials, proxy, port);
    }

    public Task<FtpClient> CreateAsync(string user = null, string pass = null, ProxyInfo proxy = null, int port = 0)
    {
        var credentials = user != null || pass != null
            ? new NetworkCredential(user, pass)
            : null;

        return CreateAsync(credentials, proxy, port);
    }

    public Task<FtpClient> CreateAsync(NetworkCredential credentials = null, bool useProxy = false, int port = 0)
    {
        var proxy = GetProxy(useProxy);

        return CreateAsync(credentials, proxy, port);
    }

    public async Task<FtpClient> CreateAsync(NetworkCredential credentials = null, ProxyInfo proxy = null, int port = 0)
    {
        await _semaphore.WaitAsync();

        try
        {
            FtpClient client;

            if (proxy != null)
                client = new FtpClientHttp11Proxy(proxy);
            else
                client = new();

            if (HostUri != null)
                client.Host = HostUri.AbsoluteUri;

            if (credentials != null)
                client.Credentials = credentials;

            if (port != 0)
                client.Port = port;

            FtpTrace.WriteLine($"FTPClient::ConnectionType = \'{client.ConnectionType}\'");

            return client;
        }
        catch
        {
            _semaphore.Release();

            throw;
        }
    }

    public async Task ReleaseClientAsync(FtpClient client)
    {
        if (!client.IsDisposed)
        {
            if (client.IsConnected)
                await client.DisconnectAsync();

            client.Dispose();
        }

        _semaphore.Release();
    }

    protected void OnValidateCertificate(FtpSslValidationEventArgs e)
    {
        e.Accept = true;
    }

    internal ProxyInfo GetProxy(bool useProxy)
    {
        if (useProxy)
            return new()
            {
                Credentials = (NetworkCredential)ProxyCredentials,
                Host = ProxyHost,
                Port = ProxyPort
            };

        return null;
    }

    #region Singleton
    private static ISynchronizedAccessService LockSrv => FExCoreStatics.SynchronizedAccessService;
    private static ConcurrentDictionary<string, FtpClientFactory> Instances { get; } = new();

    public static async Task<FtpClientFactory> GetInstanceAsync(string hostUri, int maxParallel = 5)
    {
        var semaphore = LockSrv.EnsureLock($"{hostUri}@{nameof(FtpClientFactory)}_Instance");
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