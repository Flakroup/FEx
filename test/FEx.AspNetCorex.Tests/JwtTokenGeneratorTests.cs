using FEx.AspNetCorex.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FEx.AspNetCorex.Tests;

/// <summary>
/// The access token the generator issues must validate against the same options the bearer handler
/// uses (issuer, audience, key, lifetime) and carry the standard claims unmapped.
/// </summary>
public sealed class JwtTokenGeneratorTests
{
    private const string Key = "unit-test-signing-key-0123456789-0123456789-0123456789";

    private static readonly DateTimeOffset Now = new(2026, 7, 15, 2, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.Parse("7e57ab1e-0000-4000-8000-000000000001");

    [Fact]
    public async Task GeneratedToken_Validates_AndCarriesTheStandardClaims()
    {
        var options = MakeOptions();
        JwtTokenGenerator generator = new(Options.Create(options), new FixedTime(Now));

        var (token, expires) = generator.Generate(UserId, "user@example.com");

        expires.ShouldBe(Now + options.AccessTokenLifetime);

        JsonWebTokenHandler handler = new();

        var result = await handler.ValidateTokenAsync(token,
            new()
            {
                ValidIssuer = options.Issuer,
                ValidAudience = options.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
                // The token is intentionally issued "in the past" (fixed test clock); only signature,
                // issuer and audience are under test here - expiry is asserted separately below.
                ValidateLifetime = false
            });

        result.IsValid.ShouldBeTrue(result.Exception?.Message);
        result.Claims["sub"].ShouldBe(UserId.ToString());
        result.Claims["email"].ShouldBe("user@example.com");
        result.Claims.ContainsKey("jti").ShouldBeTrue();

        var jwt = (JsonWebToken)result.SecurityToken;
        jwt.ValidTo.ShouldBe(expires.UtcDateTime);
    }

    [Fact]
    public async Task TokenSignedWithADifferentKey_FailsValidation()
    {
        JwtTokenGenerator generator = new(Options.Create(MakeOptions()), new FixedTime(Now));
        var (token, _) = generator.Generate(UserId, "user@example.com");

        JsonWebTokenHandler handler = new();

        var result = await handler.ValidateTokenAsync(token,
            new()
            {
                ValidIssuer = "Sample.Api",
                ValidAudience = "Sample.Clients",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('x', 48))),
                ValidateLifetime = false
            });

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void EveryToken_GetsAFreshJti()
    {
        JwtTokenGenerator generator = new(Options.Create(MakeOptions()), new FixedTime(Now));

        var (first, _) = generator.Generate(UserId, "a@b.pl");
        var (second, _) = generator.Generate(UserId, "a@b.pl");

        first.ShouldNotBe(second);
    }

    [Fact]
    public void MissingKey_ThrowsOnConstruction()
    {
        var options = MakeOptions();
        options.Key = string.Empty;

        Should.Throw<InvalidOperationException>(() =>
            new JwtTokenGenerator(Options.Create(options), new FixedTime(Now)));
    }

    [Fact]
    public void MissingIssuer_ThrowsOnConstruction()
    {
        var options = MakeOptions();
        options.Issuer = string.Empty;

        Should.Throw<InvalidOperationException>(() =>
            new JwtTokenGenerator(Options.Create(options), new FixedTime(Now)));
    }

    private static JwtOptions MakeOptions() =>
        new()
        {
            Key = Key,
            Issuer = "Sample.Api",
            Audience = "Sample.Clients",
            AccessTokenLifetime = TimeSpan.FromMinutes(15)
        };

    private sealed class FixedTime : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTime(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }
}