using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv;

public class OneDriveClient
{
    private readonly PublicClientApplicationOptions _appConfiguration;
    private readonly string[] _scopes;
    private IPublicClientApplication _application;

    public OneDriveClient()
    {
        _scopes = new[] { "User.Read", "Files.Read", "Files.Read.All" };
        _appConfiguration = new PublicClientApplicationOptions
        {
            Instance = "https://login.microsoftonline.com/",
            ClientId = Environment.GetEnvironmentVariable("OneDriveAppClientId", EnvironmentVariableTarget.User),
            TenantId = "common"
        };
    }

    public async Task<List<Drive>> ListDrivesAsync(CancellationToken cancellationToken = default)
    {
        var drives = new List<Drive>();

        try
        {
            GraphServiceClient client = GetGraphServiceClient();
            IUserDrivesCollectionPage r = await client.Me.Drives.Request()
                .GetAsync(cancellationToken);
            var pI = PageIterator<Drive>.CreatePageIterator(client, r, d =>
            {
                drives.Add(d);
                return true;
            });
            await pI.IterateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
        }

        return drives;
    }


    private GraphServiceClient GetGraphServiceClient()
    {
        const string msGraphURL = "https://graph.microsoft.com/v1.0/";
        var authenticationProvider = new DelegateAuthenticationProvider(AuthenticateRequestAsyncDelegate);
        return new GraphServiceClient(msGraphURL, authenticationProvider);
    }

    private async Task AuthenticateRequestAsyncDelegate(HttpRequestMessage requestMessage)
    {
        string parameter = await SignInUserAndGetTokenUsingMSAL(_appConfiguration, _scopes);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", parameter);
    }

    private async Task<string> SignInUserAndGetTokenUsingMSAL(PublicClientApplicationOptions configuration, string[] scopes)
    {
        // build the AAd authority Url
        var authority = string.Concat(configuration.Instance, configuration.TenantId);

        // Initialize the MSAL library by building a public client application
        _application = PublicClientApplicationBuilder.Create(configuration.ClientId)
            .WithAuthority(authority)
            .WithDefaultRedirectUri()
            .Build();

        AuthenticationResult result;

        try
        {
            var accounts = (await _application.GetAccountsAsync()).ToList();
            // Try to acquire an access token from the cache. If device code is required, Exception will be thrown.
            result = await _application.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                .ExecuteAsync();
        }
        catch (MsalUiRequiredException)
        {
            result = await _application.AcquireTokenWithDeviceCode(scopes, deviceCodeResult =>
                {
                    // This will print the message on the console which tells the user where to go sign-in using
                    // a separate browser and the code to enter once they sign in.
                    // The AcquireTokenWithDeviceCode() method will poll the server after firing this
                    // device code callback to look for the successful login of the user via that browser.
                    // This background polling (whose interval and timeout data is also provided as fields in the
                    // deviceCodeCallback class) will occur until:
                    // * The user has successfully logged in via browser and entered the proper code
                    // * The timeout specified by the server for the lifetime of this code (typically ~15 minutes) has been reached
                    // * The developing application calls the Cancel() method on a CancellationToken sent into the method.
                    //   If this occurs, an OperationCanceledException will be thrown (see catch below for more details).
                    Console.WriteLine(deviceCodeResult.Message);
                    return Task.FromResult(0);
                })
                .ExecuteAsync();
        }

        return result.AccessToken;
    }
}