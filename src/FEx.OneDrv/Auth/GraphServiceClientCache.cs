using FEx.OneDrv.Abstractions;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv.Auth;

public sealed class GraphServiceClientCache : IGraphServiceClientCache, IDisposable
{
    private readonly IOneDriveAuthService _auth;
    private readonly Func<IAccessTokenProvider, GraphServiceClient> _clientFactory;
    private readonly Func<GraphServiceClient, CancellationToken, Task<string>> _driveIdFactory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private (GraphServiceClient Client, string DriveId)? _cached;

    public GraphServiceClientCache(IOneDriveAuthService auth)
        : this(auth, DefaultClientFactory, DefaultDriveIdFactoryAsync)
    {
    }

    internal GraphServiceClientCache(IOneDriveAuthService auth,
                                     Func<IAccessTokenProvider, GraphServiceClient> clientFactory,
                                     Func<GraphServiceClient, CancellationToken, Task<string>> driveIdFactory)
    {
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _driveIdFactory = driveIdFactory ?? throw new ArgumentNullException(nameof(driveIdFactory));
    }

    public async Task<(GraphServiceClient Client, string DriveId)> GetAsync(CancellationToken cancellationToken)
    {
        if (_cached is { } cached)
            return cached;

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cached is { } cachedInsideLock)
                return cachedInsideLock;

            var tokenProvider = new DelegatingAccessTokenProvider(_auth.GetAccessTokenAsync);
            var client = _clientFactory(tokenProvider);
            var driveId = await _driveIdFactory(client, cancellationToken).ConfigureAwait(false);

            _cached = (client, driveId);
            return _cached.Value;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate() => _cached = null;

    public void Dispose() => _lock.Dispose();

    private static GraphServiceClient DefaultClientFactory(IAccessTokenProvider tokenProvider) =>
        new(new BaseBearerTokenAuthenticationProvider(tokenProvider));

    private static async Task<string> DefaultDriveIdFactoryAsync(GraphServiceClient client, CancellationToken cancellationToken)
    {
        var drive = await client.Me.Drive.GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return drive?.Id ?? throw new InvalidOperationException("Could not retrieve the user's default OneDrive ID");
    }
}
