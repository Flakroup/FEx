using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace FEx.AspNetCorex.Auth;

/// <summary>
/// Registers JWT bearer validation for the options' HS256 key. Standard claims stay unmapped
/// (sub/email as-is, not the legacy Microsoft URI claim types).
/// </summary>
public static class JwtAuthenticationExtensions
{
    public static AuthenticationBuilder AddJwtBearerFromOptions(this AuthenticationBuilder builder, JwtOptions options)
    {
        options.Validate();

        var keyBytes = Encoding.UTF8.GetBytes(options.Key);

        // Prevent JwtBearer from remapping standard JWT claims (sub, email, ...) to the legacy
        // Microsoft URI claim types. Global, so set before any handler is built.
        JsonWebTokenHandler.DefaultMapInboundClaims = false;

        SymmetricSecurityKey signingKey = new(keyBytes);

        return builder.AddJwtBearer(bearer =>
        {
            bearer.MapInboundClaims = false;
            // TLS terminates at the proxy/edge - the app itself listens on plain HTTP.
            bearer.RequireHttpsMetadata = false;

            bearer.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidIssuer = options.Issuer,
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = "sub"
            };
        });
    }

    /// <summary>The bound (possibly defaulted) options from the "Jwt" configuration section.</summary>
    public static JwtOptions ReadJwtOptions(this IConfiguration configuration) =>
        configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
}