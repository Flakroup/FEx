using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv.Auth;

internal sealed class StaticAccessTokenProvider : IAccessTokenProvider
{
    private readonly string _token;

    public AllowedHostsValidator AllowedHostsValidator { get; } = new();

    public StaticAccessTokenProvider(string token)
    {
        _token = token ?? throw new ArgumentNullException(nameof(token));
    }

    public Task<string> GetAuthorizationTokenAsync(Uri uri,
                                                   Dictionary<string, object> additionalAuthenticationContext,
                                                   CancellationToken cancellationToken)
        => Task.FromResult(_token);
}
