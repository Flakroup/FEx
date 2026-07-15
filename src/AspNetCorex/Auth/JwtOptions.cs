using System;
using System.Text;

namespace FEx.AspNetCorex.Auth;

/// <summary>
/// JWT signing configuration, bound from the "Jwt" section. <see cref="Key" /> is the HS256 secret
/// (>= 32 bytes) and comes from out-of-band configuration (user-secrets / environment), never the repo.
/// <see cref="Issuer" /> and <see cref="Audience" /> identify the issuing app - the consumer sets them
/// (no default carries an assumed identity), and <see cref="Validate" /> catches an unconfigured section
/// early rather than issuing tokens no bearer handler would ever accept.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Throws when the options are not usable for signing or validating tokens.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
            throw new InvalidOperationException(
                $"{SectionName}:Key is missing. Configure it via user-secrets or the environment ({SectionName}__Key).");

        var keyBytes = Encoding.UTF8.GetByteCount(Key);

        if (keyBytes < 32)
            throw new InvalidOperationException(
                $"{SectionName}:Key must be at least 32 bytes (256 bits) for HS256; it is {keyBytes} bytes.");

        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException($"{SectionName}:Issuer is required.");

        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException($"{SectionName}:Audience is required.");
    }
}