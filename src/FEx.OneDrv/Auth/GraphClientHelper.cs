using FEx.OneDrv.Abstractions;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv.Auth;

internal static class GraphClientHelper
{
    internal static async Task<GraphServiceClient> CreateAsync(IOneDriveAuthService auth, CancellationToken cancellationToken)
    {
        var token = await auth.GetAccessTokenAsync(cancellationToken);
        var provider = new BaseBearerTokenAuthenticationProvider(new StaticAccessTokenProvider(token));
        return new GraphServiceClient(provider);
    }

    internal static async Task<string> GetDefaultDriveIdAsync(GraphServiceClient client, CancellationToken cancellationToken)
    {
        var drive = await client.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        return drive?.Id ?? throw new InvalidOperationException("Could not retrieve the user's default OneDrive ID");
    }
}
