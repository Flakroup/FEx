using FEx.Json;
using FEx.Json.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FEx.Logging.GlobalLogger;

namespace FEx.EFCore;

public static class DbContextExtensions
{
    private static JsonSerializerSettings Settings { get; }

    static DbContextExtensions()
    {
        Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            MaxDepth = 1
        };

        ((List<JsonConverter>)Settings.Converters).AddRange(new JsonConverter[] { ParseStringConverter.Singleton, new VersionConverter(), new StringEnumConverter() });

        Settings.Error = (_, e) =>
        {
            LogError(e.ErrorContext.Error.Message);
            e.ErrorContext.Handled = true;
        };
    }

    public static async Task ValidateAndSaveChangesAsync<TDbContext>(this TDbContext dbContext, bool validateAllProperties = true, bool acceptAllChangesOnSuccess = true, Action<string, IReadOnlyCollection<EntityEntry>> onValidationStart = null, Action<string, EntityValidationFail> onFaultyEntity = null, Action<string, IReadOnlyCollection<EntityValidationFail>> onValidationFail = null, Action<string, IReadOnlyCollection<EntityEntry>> onValidationSuccess = null) where TDbContext : DbContext
    {
        var id = Guid.NewGuid()
            .ToString();

        bool? isSuccess = dbContext.ValidateChangedEntities(id, validateAllProperties, onValidationStart, onFaultyEntity, onValidationFail, onValidationSuccess);

        if (isSuccess is not true)
            return;

        LogInformation($"[{id}]\tSaving changes to database");
        int res = await dbContext.SaveChangesAsync(acceptAllChangesOnSuccess);
        LogInformation($"[{id}]\t{res} rows affected");
    }

    public static bool? ValidateChangedEntities<TDbContext>(this TDbContext dbContext, string id, bool validateAllProperties = true, Action<string, IReadOnlyCollection<EntityEntry>> onValidationStart = null, Action<string, EntityValidationFail> onFaultyEntity = null, Action<string, IReadOnlyCollection<EntityValidationFail>> onValidationFail = null, Action<string, IReadOnlyCollection<EntityEntry>> onValidationSuccess = null) where TDbContext : DbContext
    {
        ReadOnlyCollection<EntityEntry> entities = dbContext.GetChangedEntities()
            .AsReadOnly();

        if (entities.Count == 0)
            return null;

        var isSuccess = true;

        LogInformation($"[{id}]\tBegan {entities.Count} {(entities.Count > 1 ? "entities" : "entity")} validation");
        onValidationStart?.Invoke(id, entities); //todo convert to Rx

        if (entities.Any(x => x.State != EntityState.Deleted))
        {
            var failedValidations = new List<EntityValidationFail>();
            var failedValidation = new List<ValidationResult>();

            foreach ((EntityEntry entry, int counter) in entities.Select((entry, counter) => (entry, counter))
                         .Where(x => x.entry.State != EntityState.Deleted))
            {
                var validationContext = new ValidationContext(entry.Entity);
                failedValidation.Clear();

                if (!Validator.TryValidateObject(entry.Entity, validationContext, failedValidation, validateAllProperties))
                {
                    ReadOnlyCollection<ValidationResult> fails = failedValidation.ToList()
                        .AsReadOnly();

                    if (isSuccess)
                    {
                        isSuccess = false;
                        LogError($"[{id}]\tFAILED");
                    }

                    var fail = new EntityValidationFail(entry, fails, counter);
                    failedValidations.Add(fail);
                    onFaultyEntity?.Invoke(id, fail); //todo convert to Rx
                }
            }

            if (failedValidations.Count != 0)
            {
                var sb = new StringBuilder();
                sb.Append('[')
                    .Append(id)
                    .AppendLine("]");
                foreach (string message in failedValidations.Select(res => GetValidationResultInfo(res, Debugger.IsAttached))
                             .Distinct())
                    sb.Append(message);

                var ex = new InvalidDataException(sb.ToString());
                LogError($"[{id}]\tValidation of {failedValidations.Count} {(entities.Count > 1 ? "entities" : "entity")} failed", ex);
                onValidationFail?.Invoke(id, failedValidations); //todo convert to Rx
                throw ex;
            }
        }

        if (isSuccess)
        {
            LogInformation($"[{id}]\tValidation of {entities.Count} {(entities.Count > 1 ? "entities" : "entity")} finished successfully");
            onValidationSuccess?.Invoke(id, entities); //todo convert to Rx
        }

        return isSuccess;
    }

    public static IList<EntityEntry> GetChangedEntities<TDbContext>(this TDbContext dbContext) where TDbContext : DbContext
    {
        return dbContext.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
    }

    private static string GetValidationResultInfo(EntityValidationFail fail, bool detailedInfo = false)
    {
        int counter = fail.Index;
        EntityEntry entry = fail.Entry;
        IReadOnlyCollection<ValidationResult> failedValidations = fail.FailedValidations;

        var sb = new StringBuilder();

        if (detailedInfo)
            sb.Append('[')
                .Append(counter)
                .Append("] ");

        sb.Append("Entity of type ")
            .Append(entry.Entity.GetType()
                .Name)
            .Append(' ');

        if (Debugger.IsAttached && detailedInfo)
            sb.AppendLine()
                .AppendLine(entry.Entity.ToJson(Settings));

        sb.AppendLine("has failed validation with following errors:");

        foreach (ValidationResult val in failedValidations)
        {
            sb.Append("\t\tOn fields: ")
                .AppendLine(string.Join(", ", val.MemberNames))
                .Append("\t\tError: ")
                .AppendLine(val.ErrorMessage);
        }

        return sb.ToString();
    }
}