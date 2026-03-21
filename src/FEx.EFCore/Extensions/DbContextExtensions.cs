#if NETSTANDARD
using FEx.Agnostics.Abstractions.Extensions.Collections.Lists;
#endif
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Flow;
using FEx.EFCore.Enums;
using FEx.EFCore.Models;
using FEx.Json.Converters;
using FEx.Json.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static FEx.Agnostics.Abstractions.Logging.FExStaticLogger;

namespace FEx.EFCore.Extensions;

public static class DbContextExtensions
{
    private static JsonSerializerSettings Settings { get; }

    static DbContextExtensions()
    {
        Settings = new()
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            MaxDepth = 1
        };

        ((List<JsonConverter>)Settings.Converters).AddRange([
            ParseStringToDoubleConverter.Singleton, ParseStringToLongConverter.Singleton, new VersionConverter(),
            new StringEnumConverter()
        ]);

        Settings.Error = (_, e) =>
        {
            Error(e.ErrorContext.Error.Message);
            e.ErrorContext.Handled = true;
        };
    }

    public static Task ValidateAndSaveChangesAsync<TDbContext>(this TDbContext dbContext)
        where TDbContext : DbContext =>
        dbContext.ValidateAndSaveChangesAsync(null, true, true, null, null, null, null);

    public static async Task ValidateAndSaveChangesAsync<TDbContext>(this TDbContext dbContext,
                                                                     string id,
                                                                     bool validateAllProperties,
                                                                     bool acceptAllChangesOnSuccess,
                                                                     Action<string, IReadOnlyCollection<EntityEntry>>
                                                                         onValidationStart,
                                                                     Action<string, EntityValidationFail>
                                                                         onFaultyEntity,
                                                                     Action<string, IReadOnlyCollection<
                                                                         EntityValidationFail>> onValidationFail,
                                                                     Action<string, IReadOnlyCollection<EntityEntry>>
                                                                         onValidationSuccess)
        where TDbContext : DbContext
    {
        id ??= Guid.NewGuid().ToString();

        var result = dbContext.ValidateChangedEntities(id,
            validateAllProperties,
            onValidationStart,
            onFaultyEntity,
            onValidationFail,
            onValidationSuccess);

        if (result.IsFailure)
            return;

        Information($"[{id}]\tSaving changes to database");
        var res = await dbContext.SaveChangesAsync(acceptAllChangesOnSuccess);
        Information($"[{id}]\t{res} rows affected");
    }

    public static Result<Error> ValidateChangedEntities<TDbContext>(this TDbContext dbContext)
        where TDbContext : DbContext =>
        dbContext.ValidateChangedEntities(null, true, null, null, null, null);

    public static Result<Error> ValidateChangedEntities<TDbContext>(this TDbContext dbContext,
                                                                    string id,
                                                                    bool validateAllProperties,
                                                                    Action<string, IReadOnlyCollection<EntityEntry>>
                                                                        onValidationStart,
                                                                    Action<string, EntityValidationFail>
                                                                        onFaultyEntity,
                                                                    Action<string, IReadOnlyCollection<
                                                                        EntityValidationFail>> onValidationFail,
                                                                    Action<string, IReadOnlyCollection<EntityEntry>>
                                                                        onValidationSuccess)
        where TDbContext : DbContext
    {
        id ??= Guid.NewGuid().ToString();

        var entities = dbContext.GetChangedEntities()
#if NETSTANDARD
            .ToReadOnly();
#else
            .AsReadOnly();
#endif

        if (entities.Count == 0)
            return Result<Error>.Failure;

        var isSuccess = true;

        Information($"[{id}]\tBegan {entities.Count} {(entities.Count > 1 ? "entities" : "entity")} validation");
        onValidationStart?.Invoke(id, entities); //todo convert to Rx

        if (entities.Any(x => x.State != EntityState.Deleted))
        {
            var allFailedValidations = new List<EntityValidationFail>();
            var failedValidations = new List<ValidationResult>();

            foreach (var (entry, counter) in entities.Select((entry, counter) => (entry, counter))
                         .Where(x => x.entry.State != EntityState.Deleted))
            {
                var validationContext = new ValidationContext(entry.Entity);
                failedValidations.Clear();

                if (!Validator.TryValidateObject(entry.Entity,
                        validationContext,
                        failedValidations,
                        validateAllProperties))
                {
                    var fails = failedValidations.ToList().AsReadOnly();

                    if (isSuccess)
                    {
                        isSuccess = false;
                        Error($"[{id}]\tFAILED");
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

                foreach (var message in allFailedValidations
                             .Select(res => GetValidationResultInfo(res, Debugger.IsAttached))
                             .Distinct())
                    sb.Append(message);

                var ex = new InvalidDataException(sb.ToString());

                Error(ex,
                    $"[{id}]\tValidation of {allFailedValidations.Count} {(entities.Count > 1 ? "entities" : "entity")} failed");

                onValidationFail?.Invoke(id, allFailedValidations); //todo convert to Rx

                throw ex;
            }
        }

        if (isSuccess)
        {
            Information(
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
        var exists = await dbSet.AsNoTracking().AnyAsync(existenceFunc);

        if (exists)
            dbSet.Update(data);
        else
            dbSet.Add(data);
    }

    public static IList<EntityEntry> GetChangedEntities<TDbContext>(this TDbContext dbContext)
        where TDbContext : DbContext =>
    [
        .. dbContext.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
    ];

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

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "commandText must be a trusted, developer-authored SQL query. Callers must not pass user-controlled strings.")]
    public static string ExecuteReader<TDbContext>(this TDbContext db, string commandText, params DbParameter[] parameters)
        where TDbContext : DbContext
    {
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = commandText;

        if (parameters.Length > 0)
            command.Parameters.AddRange(parameters);

        db.Database.OpenConnection();
        using var reader = command.ExecuteReader();
        var sb = new StringBuilder();

        while (reader.Read())
            sb.Append(reader.GetString(0));

        var result = sb.ToString();

        return result;
    }

    public static string GetJsonCommand<TDbContext, T>(this TDbContext context) where TDbContext : DbContext
    {
        var (tableName, properties) = context.GetSerializedPropertiesString<TDbContext, T>();

        return context.IsSqlite()
            ? "SELECT\r\n"
              + "json_group_array(\r\n"
              + $"json_object({properties})\r\n"
              + ") AS json_result\r\n"
              + $"FROM (SELECT * FROM {tableName});"
            : $"SELECT * FROM {tableName} FOR JSON AUTO";
    }

    public static void EnsureCreatingMissingTables<TDbContext>(this TDbContext dbContext) where TDbContext : DbContext
    {
        var type = typeof(TDbContext);
        var dbSetType = typeof(DbSet<>);

        string[] dbPropertyNames =
            [.. type.GetProperties().Where(p => p.PropertyType.Name == dbSetType.Name).Select(p => p.Name)];

        foreach (var entityName in dbPropertyNames)
            CheckTableExistsAndCreateIfMissing(dbContext, entityName);
    }

    private static (string tableName, string properties)
        GetSerializedPropertiesString<TDbContext, T>(this TDbContext dbContext) where TDbContext : DbContext
    {
        var entityName = typeof(T).FullName;
        var entityType = dbContext.Model.GetEntityTypes().First(x => x.Name == entityName);
        var tableName = entityType.GetTableName();

        string[] columnNames = [.. entityType.GetProperties().Select(propertyType => propertyType.GetColumnName())];

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
        var counter = fail.Index;
        var entry = fail.Entry;
        var failedValidations = fail.FailedValidations;

        var sb = new StringBuilder();

        if (detailedInfo)
            sb.Append('[').Append(counter).Append("] ");

        sb.Append("Entity of type ").Append(entry.Entity.GetType().Name).Append(' ');

        if (Debugger.IsAttached && detailedInfo)
            sb.AppendLine().AppendLine(entry.Entity.ToJson(Settings));

        sb.AppendLine("has failed validation with following errors:");

        foreach (var val in failedValidations)
        {
            sb.Append("\t\tOn fields: ")
                .AppendLine(string.Join(", ", val.MemberNames))
                .Append("\t\tError: ")
                .AppendLine(val.ErrorMessage);
        }

        return sb.ToString();
    }

    private static void CheckTableExistsAndCreateIfMissing(DbContext dbContext, string entityName)
    {
        var defaultSchema = dbContext.Model.GetDefaultSchema();

        var tableName = string.IsNullOrWhiteSpace(defaultSchema)
            ? $"[{entityName}]"
            : $"[{defaultSchema}].[{entityName}]";

        try
        {
#pragma warning disable EF1002 // tableName is built from EF model metadata, not user input
            _ = dbContext.Database.ExecuteSqlRaw($"SELECT TOP(1) * FROM {tableName}");
#pragma warning restore EF1002
        }
        catch (Exception)
        {
            var scriptStart = $"CREATE TABLE {tableName}";
            const string scriptEnd = "GO";
            var script = dbContext.Database.GenerateCreateScript();

            var tableScript = script.Split([scriptStart], StringSplitOptions.RemoveEmptyEntries)
                .Last()
                .Split([scriptEnd], StringSplitOptions.RemoveEmptyEntries);

            var first = $"{scriptStart} {tableScript.First()}";

            dbContext.Database.ExecuteSqlRaw(first);
            Log.Information($"Database table: '{tableName}' was created.");
        }
    }
}