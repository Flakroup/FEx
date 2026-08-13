using System.Collections.Generic;

namespace FEx.OneDrv.Abstractions;

public sealed class OneDriveOptions
{
    // Must be set by the consumer (e.g. via configuration binding) before use.
    public string ClientId { get; set; } = null!;
    public string TenantId { get; set; } = "common";
    public IList<string> Scopes { get; set; } = ["User.Read", "Files.Read", "Files.Read.All"];
    // Must be set by the consumer (e.g. via configuration binding) before use.
    public string TokenCachePath { get; set; } = null!;
}