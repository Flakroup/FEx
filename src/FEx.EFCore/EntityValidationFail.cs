using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FEx.EFCore;

public class EntityValidationFail
{
    public EntityEntry Entry { get; }
    public IReadOnlyCollection<ValidationResult> FailedValidations { get; }
    public int Index { get; }

    public EntityValidationFail(EntityEntry entry, IReadOnlyCollection<ValidationResult> failedValidations, int index)
    {
        Entry = entry;
        FailedValidations = failedValidations;
        Index = index;
    }
}