using System.Collections.Generic;

namespace FEx.OneDrv.Abstractions;

public sealed class OneDriveOptions
{
    public string ClientId { get; set; }
    public string TenantId { get; set; } = "common";
    public IList<string> Scopes { get; set; } = ["User.Read", "Files.Read", "Files.Read.All"];
    public string TokenCachePath { get; set; }
}
