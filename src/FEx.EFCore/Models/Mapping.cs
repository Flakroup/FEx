using System.Collections.Generic;

namespace FEx.EFCore.Models;

public record Mapping
{
    // All three are always populated via object initializer in PooledDbService.EnsureMappingSnapshotAsync.
    public string ClrTypeName { get; init; } = null!;
    public string TableName { get; init; } = null!;
    public IReadOnlyCollection<string> Properties { get; init; } = null!;

    public override string ToString() => TableName;
}