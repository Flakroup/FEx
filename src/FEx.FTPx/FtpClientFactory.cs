using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions;
using FluentFTP;
using FluentFTP.Proxy.AsyncProxy;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.FTPx;

public class FtpClientFactory
{
    private static readonly ConditionalWeakTable<AsyncFtpClient, FtpClientFactory> Owners = new();
    private readonly SemaphoreSlim _semaphore;
    /// <summary>
    ///     The data connection type of a client unless the caller passes another one: <see cref="FtpDataConnectionType.PASVEX" /> connects the
    ///     data channel to the control-connection host and takes only the port from the PASV reply, so a hostile or misconfigured server cannot
    ///     aim the client at another host (the port-scan/bounce class of curl CVE-2020-8284). The cost: two kinds of server fail. One is an IPv4
    ///     server that answers EPSV but not PASV, because EPSV is never tried on an IPv4 control connection. The other is a server whose FEAT
    ///     lacks EPSV and whose passive data listener is only on the other routable address its PASV reply names (one behind a load-balancer
    ///     VIP, say), because the data channel goes to the control host instead. Against either, <c>FtpCommon.GetFtpFileInfoAsync</c> returns
    ///     null, the same answer as a file that does not exist, and <c>FtpCommon.GetListingAsync</c> throws. For such a server pass
    ///     <c>dataConnectionType: </c><see cref="FtpDataConnectionType.AutoPassive" /> (or <see cref="FtpDataConnectionType.EPSV" /> for the
    ///     EPSV-only one), or set <c>client.Config.DataConnectionType</c> on the returned client before it connects.
    /// </summary>
    public const FtpDataConnectionType DefaultDataConnectionType = FtpDataConnectionType.PASVEX;

    public Uri HostUri { get; }
    public ICredentials ProxyCredentials { get; set; } = CredentialCache.DefaultCredentials;
    public string? ProxyHost { get; set; }
    public int ProxyPort { get; set; }

    public Task<AsyncFtpClient> CreateAsync(string? user = null, string? pass = null, bool useProxy = false, int port = 0, FtpDataConnectionType? dataConnectionType = null)
    {
        var credentials = user != null || pass != null
            ? new NetworkCredential(user, pass)
            : null;

        var proxy = GetProxy(useProxy);

        return CreateAsync(credentials, proxy, port, dataConnectionType);
    }

    public Task<AsyncFtpClient> CreateAsync(string? user = null, string? pass = null, FtpProxyProfile? proxy = null, int port = 0, FtpDataConnectionType? dataConnectionType = null)
    {
        var credentials = user != null || pass != null
            ? new NetworkCredential(user, pass)
            : null;

        return CreateAsync(credentials, proxy, port, dataConnectionType);
    }

    public Task<AsyncFtpClient> CreateAsync(NetworkCredential? credentials = null, bool useProxy = false, int port = 0, FtpDataConnectionType? dataConnectionType = null)
    {
        var proxy = GetProxy(useProxy);

        return CreateAsync(credentials, proxy, port, dataConnectionType);
    }

    public Task<AsyncFtpClient> CreateAsync(NetworkCredential? credentials = null, FtpProxyProfile? proxy = null, int port = 0, FtpDataConnectionType? dataConnectionType = null) =>
        CreateCoreAsync(credentials, proxy, port, dataConnectionType, CancellationToken.None);

    /// <summary>
    ///     Waits for a free slot of the host's pool, and stops waiting when <paramref name="cancellationToken" /> is cancelled. A null
    ///     <paramref name="dataConnectionType" /> means <see cref="DefaultDataConnectionType" />.
    /// </summary>
    internal async Task<AsyncFtpClient> CreateCoreAsync(NetworkCredential? credentials, FtpProxyProfile? proxy, int port, FtpDataConnectionType? dataConnectionType, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            AsyncFtpClient client;

            if (proxy != null)
                client = new AsyncFtpClientHttp11Proxy(proxy);
            else
                client = new();

            // FluentFTP keeps everything after the scheme in Host, so only the bare host goes there; the port is separate.
            client.Host = HostUri.DnsSafeHost;

            if (!HostUri.IsDefaultPort)
                client.Port = HostUri.Port;

            if (credentials != null)
                client.Credentials = credentials;

            if (port != 0)
                client.Port = port;

            client.Config.DataConnectionType = dataConnectionType ?? DefaultDataConnectionType;

            Owners.Add(client, this);

            return client;
        }
        catch
        {
            _semaphore.Release();

            throw;
        }
    }

    /// <summary>Releases a client to the factory that created it; a client no factory created is only disconnected and disposed.</summary>
    internal static Task ReleaseOwnedAsync(AsyncFtpClient client) =>
        Owners.TryGetValue(client, out var factory)
            ? factory.ReleaseClientAsync(client)
            : DisconnectAndDisposeAsync(client);

    public async Task ReleaseClientAsync(AsyncFtpClient client)
    {
        await DisconnectAndDisposeAsync(client);

        _semaphore.Release();
    }

    private static async Task DisconnectAndDisposeAsync(AsyncFtpClient client)
    {
        if (!client.IsDisposed)
        {
            if (client.IsConnected)
                await client.Disconnect();

#pragma warning disable IDISP007 // factory release pattern, client created by CreateAsync
            await client.DisposeAsync();
#pragma warning restore IDISP007
        }
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