using FEx.Utilities.Collections;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace FEx.EFCore;

public interface IPooledDbService<out TDbContext> where TDbContext : DbContext
{
    /// <summary>
    ///     Map of model to DB mappings.
    ///     ForwardIndex contains model entities names to table names in DB mapping.
    ///     ReverseIndex contains table names in DB to model entities names mapping.
    /// </summary>
    /// <value>
    ///     The mappings.
    /// </value>
    Map<string, string> Mappings { get; }

    Task InitializeAsync(Func<TDbContext, Task> afterAppliedMigration = null, bool dropIfMigrationFailed = false);
    Task RunMigrationsAsync(Func<TDbContext, Task> afterAppliedMigration = null, bool dropIfMigrationFailed = false);
    Task MigrateAsync(Func<TDbContext, Task> afterAppliedMigration = null);
    void RunActionInDbContext(Action<TDbContext> func, string errorMessage = null, bool saveChanges = true, bool useTransaction = true);
    T RunFuncInDbContext<T>(Func<TDbContext, T> func, string errorMessage = null, bool saveChanges = true, bool useTransaction = true);
    Task RunTaskInDbContextAsync(Func<TDbContext, Task> func, string errorMessage = null, bool saveChanges = true, bool useTransaction = true);
    Task<T> RunTaskInDbContextAsync<T>(Func<TDbContext, Task<T>> func, string errorMessage = null, bool saveChanges = true, bool useTransaction = true);
    Task<T> RunTaskInDbContextAsync<T>(Func<TDbContext, Func<Task<T>>> func, string errorMessage = null, bool saveChanges = true, bool useTransaction = true);
}