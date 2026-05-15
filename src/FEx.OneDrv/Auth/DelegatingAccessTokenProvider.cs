using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv.Auth;

internal sealed class DelegatingAccessTokenProvider : IAccessTokenProvider
{
    private readonly Func<CancellationToken, Task<string>> _tokenFactory;

    public AllowedHostsValidator AllowedHostsValidator { get; } = new();

    public DelegatingAccessTokenProvider(Func<CancellationToken, Task<string>> tokenFactory)
    {
        _tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
    }

    public Task<string> GetAuthorizationTokenAsync(Uri uri,
                                                   Dictionary<string, object> additionalAuthenticationContext,
                                                   CancellationToken cancellationToken)
        => _tokenFactory(cancellationToken);
}
