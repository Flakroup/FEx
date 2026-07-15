using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FEx.AspNetCorex.Auth;

/// <summary>
/// Issues short-lived HS256 access tokens with the standard claims (sub, email, jti, iat). The
/// caller supplies the user identity - the generator has no user-store dependency.
/// </summary>
public sealed class JwtTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _time;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider time)
    {
        options.Value.Validate();
        _options = options.Value;
        _time = time;
    }

    public (string AccessToken, DateTimeOffset ExpiresAtUtc) Generate(Guid userId, string email)
    {
        var now = _time.GetUtcNow();
        var expires = now.Add(_options.AccessTokenLifetime);
        var keyBytes = Encoding.UTF8.GetBytes(_options.Key);
        SigningCredentials signingCredentials = new(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = signingCredentials,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = userId.ToString(),
                ["email"] = email,
                ["jti"] = Guid.NewGuid().ToString(),
                ["iat"] = now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
            }
        };

        return (_handler.CreateToken(descriptor), expires);
    }
}