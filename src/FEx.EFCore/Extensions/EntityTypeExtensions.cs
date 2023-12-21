using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;
using System.Linq;

namespace FEx.EFCore.Extensions;

public static class EntityTypeExtensions
{
    public static IReadOnlyCollection<string> GetMappedProperties(this IEntityType entityType)
    {
        return entityType.GetProperties().Select(propertyType => propertyType.Name).ToList().AsReadOnly();
    }
}