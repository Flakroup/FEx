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
    private IPublicClientApplication _app;

    public MsalAuthService(OneDriveOptions options, IFExLogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var app = await GetOrBuildAppAsync();
        var scopes = _options.Scopes.ToArray();

        var accounts = await app.GetAccountsAsync();
        try
        {
            var result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                                  .ExecuteAsync(cancellationToken);
            return result.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            _logger.Information("Silent token acquisition failed - falling back to interactive");
        }

        var interactiveResult = await app.AcquireTokenInteractive(scopes)
                                         .ExecuteAsync(cancellationToken);
        return interactiveResult.AccessToken;
    }

    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        var app = await GetOrBuildAppAsync();
        var accounts = (await app.GetAccountsAsync()).ToList();
        foreach (var account in accounts)
            await app.RemoveAsync(account);

        _logger.Information("Signed out all OneDrive accounts");
    }

    private async Task<IPublicClientApplication> GetOrBuildAppAsync()
    {
        if (_app != null)
            return _app;

        var builder = PublicClientApplicationBuilder.Create(_options.ClientId)
            .WithAuthority($"https://login.microsoftonline.com/{_options.TenantId}")
            .WithDefaultRedirectUri();

        _app = builder.Build();

        if (!string.IsNullOrWhiteSpace(_options.TokenCachePath))
            await RegisterTokenCacheAsync(_app.UserTokenCache);

        return _app;
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
