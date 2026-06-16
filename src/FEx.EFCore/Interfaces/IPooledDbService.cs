using FEx.Agnostics.Abstractions.Collections;
using FEx.Core.Abstractions.Interfaces;
using FEx.EFCore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.EFCore.Interfaces;

public interface IPooledDbService<out TDbContext> : IAsyncInitializable where TDbContext : DbContext
{
    /// <summary>
    /// Map of model to DB mappings.
    /// ForwardIndex contains model entities names to table names in DB mapping.
    /// ReverseIndex contains table names in DB to model entities names mapping.
    /// </summary>
    /// <value>
    /// The mappings.
    /// </value>
    Map<string, string> TableMappings { get; }

    IReadOnlyDictionary<string, Mapping> Mappings { get; }

    Task<bool> RunMigrationsAsync();
    Task MigrateAsync();

    Task RunActionInDbContextAsync(Action<TDbContext> func, string errorMessage, bool saveChanges, bool useTransaction);

    Task<T> RunFuncInDbContextAsync<T>(Func<TDbContext, T> func,
                                       string errorMessage = null,
                                       bool saveChanges = true,
                                       bool useTransaction = true);

    Task RunTaskInDbContextAsync(Func<TDbContext, Task> func,
                                 string errorMessage = null,
                                 bool saveChanges = true,
                                 bool useTransaction = true);

    Task<T> RunTaskInDbContextAsync<T>(Func<TDbContext, Task<T>> func,
                                       string errorMessage = null,
                                       bool saveChanges = true,
                                       bool useTransaction = true);

    Task<T> RunTaskInDbContextAsync<T>(Func<TDbContext, Func<Task<T>>> func,
                                       string errorMessage = null,
                                       bool saveChanges = true,
                                       bool useTransaction = true);
}