using FEx.Agnostics.Abstractions.Interfaces;
using FEx.OneDrv.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv;

/// <summary>MSAL-based <see cref="IOneDriveAuthService"/>. Options are validated and copied at construction.</summary>
public sealed class MsalAuthService : IOneDriveAuthService
{
    private readonly string _clientId;
    private readonly string _tenantId;
    private readonly string? _tokenCachePath;
    private readonly string[] _scopes;
    private readonly IFExLogger _logger;
    private readonly SemaphoreSlim _appLock = new(1, 1);
    private IPublicClientApplication? _app;

    public MsalAuthService(OneDriveOptions options, IFExLogger logger)
    {
        _ = options ?? throw new ArgumentNullException(nameof(options));
        options.Validate();

        // Snapshot so later edits to the (mutable) options cannot diverge from the app built from them.
        _clientId = options.ClientId;
        _tenantId = options.TenantId;
        _tokenCachePath = options.TokenCachePath;
        _scopes = options.Scopes.ToArray();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var app = await GetOrBuildAppAsync(cancellationToken);
        var scopes = _scopes;

        var accounts = await app.GetAccountsAsync();
        var firstAccount = accounts.FirstOrDefault();

        if (firstAccount != null)
            try
            {
                var result = await app.AcquireTokenSilent(scopes, firstAccount).ExecuteAsync(cancellationToken);

                return result.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                _logger.Information("Silent token acquisition failed - falling back to interactive");
            }

        var interactiveResult = await app.AcquireTokenInteractive(scopes).ExecuteAsync(cancellationToken);

        return interactiveResult.AccessToken;
    }

    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        var app = await GetOrBuildAppAsync(cancellationToken);
        var accounts = (await app.GetAccountsAsync()).ToList();

        foreach (var account in accounts)
            await app.RemoveAsync(account);

        _logger.Information("Signed out all OneDrive accounts");
    }

    private async Task<IPublicClientApplication> GetOrBuildAppAsync(CancellationToken cancellationToken)
    {
        if (_app != null)
            return _app;

        await _appLock.WaitAsync(cancellationToken);

        try
        {
            if (_app != null)
                return _app;

            var builder = PublicClientApplicationBuilder.Create(_clientId)
                .WithAuthority($"https://login.microsoftonline.com/{_tenantId}")
                .WithDefaultRedirectUri();

            var app = builder.Build();

            if (!string.IsNullOrWhiteSpace(_tokenCachePath))
                await RegisterTokenCacheAsync(app.UserTokenCache);

            _app = app;

            return _app;
        }
        finally
        {
            _appLock.Release();
        }
    }

    private async Task RegisterTokenCacheAsync(ITokenCache tokenCache)
    {
        var cacheDir = Path.GetDirectoryName(_tokenCachePath) ?? string.Empty;
        var cacheFileName = Path.GetFileName(_tokenCachePath);

        var storageProperties = new StorageCreationPropertiesBuilder(cacheFileName, cacheDir).Build();

        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(tokenCache);
    }
}