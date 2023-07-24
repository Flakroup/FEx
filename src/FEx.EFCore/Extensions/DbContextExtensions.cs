#if NETSTANDARD
using FEx.Extensions.Collections.Lists;
#endif
using FEx.Basics.Flow;
using FEx.EFCore.Enums;
using FEx.EFCore.Models;
using FEx.Extensions;
using FEx.Json;
using FEx.Json.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static FEx.Logging.GlobalLogger;

namespace FEx.EFCore.Extensions;

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

        ((List<JsonConverter>)Settings.Converters).AddRange(new JsonConverter[]
        {
            ParseStringConverter.Singleton, new VersionConverter(), new StringEnumConverter()
        });

        Settings.Error = (_, e) =>
        {
            LogError(e.ErrorContext.Error.Message);
            e.ErrorContext.Handled = true;
        };
    }

    public static async Task ValidateAndSaveChangesAsync<TDbContext>(this TDbContext dbContext,
                                                                     string id = null,
                                                                     bool validateAllProperties = true,
                                                                     bool acceptAllChangesOnSuccess = true,
                                                                     Action<string, IReadOnlyCollection<EntityEntry>>
                                                                         onValidationStart = null,
                                                                     Action<string, EntityValidationFail>
                                                                         onFaultyEntity = null,
                                                                     Action<string, IReadOnlyCollection<
                                                                         EntityValidationFail>> onValidationFail = null,
                                                                     Action<string, IReadOnlyCollection<EntityEntry>>
                                                                         onValidationSuccess = null)
        where TDbContext : DbContext
    {
        id ??= Guid.NewGuid().ToString();

        Result<Error> result = dbContext.ValidateChangedEntities(id, validateAllProperties, onValidationStart,
            onFaultyEntity, onValidationFail, onValidationSuccess);

        if (result.IsFailure)
            return;

        LogInformation($"[{id}]\tSaving changes to database");
        int res = await dbContext.SaveChangesAsync(acceptAllChangesOnSuccess);
        LogInformation($"[{id}]\t{res} rows affected");
    }

    public static Result<Error> ValidateChangedEntities<TDbContext>(this TDbContext dbContext,
                                                                    string id = null,
                                                                    bool validateAllProperties = true,
                                                                    Action<string, IReadOnlyCollection<EntityEntry>>
                                                                        onValidationStart = null,
                                                                    Action<string, EntityValidationFail>
                                                                        onFaultyEntity = null,
                                                                    Action<string, IReadOnlyCollection<
                                                                        EntityValidationFail>> onValidationFail = null,
                                                                    Action<string, IReadOnlyCollection<EntityEntry>>
                                                                        onValidationSuccess = null)
        where TDbContext : DbContext
    {
        id ??= Guid.NewGuid().ToString();

        ReadOnlyCollection<EntityEntry> entities = dbContext.GetChangedEntities()
#if NETSTANDARD
            .ToReadOnly();
#else
            .AsReadOnly();
#endif

        if (entities.Count == 0)
            return Result<Error>.Failure;

        var isSuccess = true;

        LogInformation($"[{id}]\tBegan {entities.Count} {(entities.Count > 1 ? "entities" : "entity")} validation");
        onValidationStart?.Invoke(id, entities); //todo convert to Rx

        if (entities.Any(x => x.State != EntityState.Deleted))
        {
            var allFailedValidations = new List<EntityValidationFail>();
            var failedValidations = new List<ValidationResult>();

            foreach ((EntityEntry entry, int counter) in entities.Select((entry, counter) => (entry, counter))
                         .Where(x => x.entry.State != EntityState.Deleted))
            {
                var validationContext = new ValidationContext(entry.Entity);
                failedValidations.Clear();

                if (!Validator.TryValidateObject(entry.Entity, validationContext, failedValidations,
                        validateAllProperties))
                {
                    ReadOnlyCollection<ValidationResult> fails = failedValidations.ToList().AsReadOnly();

                    if (isSuccess)
                    {
                        isSuccess = false;
                        LogError($"[{id}]\tFAILED");
                    }

                    var fail = new EntityValidationFail(entry, fails, counter);
                    allFailedValidations.Add(fail);
                    onFaultyEntity?.Invoke(id, fail); //todo convert to Rx
                }
            }

            if (allFailedValidations.Count != 0)
            {
                var sb = new StringBuilder();
                sb.Append('[').Append(id).AppendLine("]");
                foreach (string message in allFailedValidations
                             .Select(res => GetValidationResultInfo(res, Debugger.IsAttached))
                             .Distinct())
                    sb.Append(message);

                var ex = new InvalidDataException(sb.ToString());
                LogError(
                    $"[{id}]\tValidation of {allFailedValidations.Count} {(entities.Count > 1 ? "entities" : "entity")} failed",
                    ex);
                onValidationFail?.Invoke(id, allFailedValidations); //todo convert to Rx
                throw ex;
            }
        }

        if (isSuccess)
        {
            LogInformation(
                $"[{id}]\tValidation of {entities.Count} {(entities.Count > 1 ? "entities" : "entity")} finished successfully");
            onValidationSuccess?.Invoke(id, entities); //todo convert to Rx
        }

        return isSuccess
            ? Result<Error>.Success
            : Result<Error>.Failure;
    }

    public static async Task AddOrUpdateAsync<T>(this DbSet<T> dbSet, T data, Expression<Func<T, bool>> existenceFunc)
        where T : class
    {
        bool exists = await dbSet.AsNoTracking().AnyAsync(existenceFunc);

        if (exists)
            dbSet.Update(data);
        else
            dbSet.Add(data);
    }

    public static IList<EntityEntry> GetChangedEntities<TDbContext>(this TDbContext dbContext)
        where TDbContext : DbContext
    {
        return dbContext.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
    }

    public static bool IsSqlite<TDbContext>(this TDbContext context) where TDbContext : DbContext =>
        context.Database.ProviderName?.EndsWith(nameof(SqlDialect.Sqlite)) == true;

    public static bool IsMySql<TDbContext>(this TDbContext context) where TDbContext : DbContext =>
        context.Database.ProviderName?.EndsWith(nameof(SqlDialect.MySql)) == true;

    public static bool IsPostrgeSql<TDbContext>(this TDbContext context) where TDbContext : DbContext =>
        context.Database.ProviderName?.EndsWith(nameof(SqlDialect.PostrgeSql)) == true;

    public static bool IsSqlServer<TDbContext>(this TDbContext context) where TDbContext : DbContext =>
        context.Database.ProviderName?.EndsWith(nameof(SqlDialect.SqlServer)) == true;

    public static SqlDialect? GetProvider<TDbContext>(this TDbContext context) where TDbContext : DbContext =>
        context.IsMySql() ? SqlDialect.MySql :
        context.IsSqlServer() ? SqlDialect.SqlServer :
        context.IsPostrgeSql() ? SqlDialect.PostrgeSql :
        context.IsSqlite() ? SqlDialect.Sqlite : null;

    public static string ExecuteReader<TDbContext>(this TDbContext db, string commandText) where TDbContext : DbContext
    {
        using DbCommand command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = commandText;
        db.Database.OpenConnection();
        using DbDataReader reader = command.ExecuteReader();
        var sb = new StringBuilder();

        while (reader.Read())
            sb.Append(reader.GetString(0));

        var result = sb.ToString();

        return result;
    }

    public static string GetJsonCommand<TDbContext, T>(this TDbContext context) where TDbContext : DbContext
    {
        (string tableName, string properties) = context.GetSerializedPropertiesString<TDbContext, T>();

        return context.IsSqlite()
            ? "SELECT\r\n"
              + "json_group_array(\r\n"
              + $"json_object({properties})\r\n"
              + ") AS json_result\r\n"
              + $"FROM (SELECT * FROM {tableName});"
            : $"SELECT * FROM {tableName} FOR JSON AUTO";
    }

    private static (string tableName, string properties)
        GetSerializedPropertiesString<TDbContext, T>(this TDbContext dbContext) where TDbContext : DbContext
    {
        string entityName = typeof(T).FullName;
        IEntityType entityType = dbContext.Model.GetEntityTypes().First(x => x.Name == entityName);
        string tableName = entityType.GetTableName();
        string[] columnNames = entityType.GetProperties()
            .Select(propertyType => propertyType.GetColumnName())
            .ToArray();

        var sb = new StringBuilder();
        for (var index = 0; index < columnNames.Length; index++)
        {
            sb.Append('\'').Append(columnNames[index].FirstCharToLower()).Append("', ").Append(columnNames[index]);

            if (index < columnNames.Length - 1)
                sb.Append(',');
        }

        return (tableName, sb.ToString());
    }

    private static string GetValidationResultInfo(EntityValidationFail fail, bool detailedInfo = false)
    {
        int counter = fail.Index;
        EntityEntry entry = fail.Entry;
        IReadOnlyCollection<ValidationResult> failedValidations = fail.FailedValidations;

        var sb = new StringBuilder();

        if (detailedInfo)
            sb.Append('[').Append(counter).Append("] ");

        sb.Append("Entity of type ").Append(entry.Entity.GetType().Name).Append(' ');

        if (Debugger.IsAttached && detailedInfo)
            sb.AppendLine().AppendLine(entry.Entity.ToJson(Settings));

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