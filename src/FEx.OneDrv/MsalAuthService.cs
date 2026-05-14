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

public sealed class MsalAuthService : IOneDriveAuthService
{
    private readonly OneDriveOptions _options;
    private readonly IFExLogger _logger;
    private readonly SemaphoreSlim _appLock = new(1, 1);
    private IPublicClientApplication _app;

    public MsalAuthService(OneDriveOptions options, IFExLogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var app = await GetOrBuildAppAsync(cancellationToken);
        var scopes = _options.Scopes.ToArray();

        var accounts = await app.GetAccountsAsync();
        var firstAccount = accounts.FirstOrDefault();

        if (firstAccount != null)
        {
            try
            {
                var result = await app.AcquireTokenSilent(scopes, firstAccount)
                                      .ExecuteAsync(cancellationToken);
                return result.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                _logger.Information("Silent token acquisition failed - falling back to interactive");
            }
        }

        var interactiveResult = await app.AcquireTokenInteractive(scopes)
                                         .ExecuteAsync(cancellationToken);
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

            var builder = PublicClientApplicationBuilder.Create(_options.ClientId)
                .WithAuthority($"https://login.microsoftonline.com/{_options.TenantId}")
                .WithDefaultRedirectUri();

            var app = builder.Build();

            if (!string.IsNullOrWhiteSpace(_options.TokenCachePath))
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
        var cacheDir = Path.GetDirectoryName(_options.TokenCachePath) ?? string.Empty;
        var cacheFileName = Path.GetFileName(_options.TokenCachePath);

        var storageProperties = new StorageCreationPropertiesBuilder(cacheFileName, cacheDir)
            .Build();

        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(tokenCache);
    }
}
