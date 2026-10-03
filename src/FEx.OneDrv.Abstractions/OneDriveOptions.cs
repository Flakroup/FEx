using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.OneDrv.Abstractions;

/// <summary>Configuration of the OneDrive module.</summary>
public sealed class OneDriveOptions
{
    /// <summary>Azure AD application (client) ID. Required: options must be fully populated (e.g. bound from configuration) before they are passed to <c>AddOneDrv</c>, which validates them.</summary>
    public string ClientId { get; set; } = null!;

    /// <summary>Azure AD tenant; <c>common</c> by default.</summary>
    public string TenantId { get; set; } = "common";

    /// <summary>Requested Graph scopes. Services copy them at construction, so later edits have no effect.</summary>
    public IList<string> Scopes { get; set; } = ["User.Read", "Files.Read", "Files.Read.All"];

    /// <summary>Token cache file path. Optional; thumbnails fall back to the temp directory when unset.</summary>
    public string TokenCachePath { get; set; } = null!;

    /// <summary>Throws <see cref="ArgumentException"/> when the options cannot be used to sign in.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
            throw new ArgumentException("OneDriveOptions.ClientId must be set to the Azure AD application (client) ID.",
                nameof(ClientId));

        if (string.IsNullOrWhiteSpace(TenantId))
            throw new ArgumentException("OneDriveOptions.TenantId must not be empty.", nameof(TenantId));

        if (Scopes is null || Scopes.Count == 0 || Scopes.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("OneDriveOptions.Scopes must contain at least one non-empty scope.",
                nameof(Scopes));
    }
}
