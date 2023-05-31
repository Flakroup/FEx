using System.Collections.Generic;

namespace FEx.EFCore.Models;

public record Mapping
{
    public string ClrTypeName { get; init; }
    public string TableName { get; init; }
    public IReadOnlyCollection<string> Properties { get; init; }

    public override string ToString() => TableName;
}