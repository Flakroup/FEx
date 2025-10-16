using FEx.Core.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FEx.EFCore.Services;

/// <summary>
/// </summary>
/// <typeparam name="TDbContext"></typeparam>
/// <remarks>Requires <c>Initialize();</c> call in .ctor</remarks>
public abstract class DbServiceBase<TDbContext> : PooledDbService<TDbContext>, IDbServiceBase<TDbContext>
    where TDbContext : DbContext
{
    protected DbServiceBase(IScopeProvider scopeProvider,
                            ResilientTransaction resilientTransaction,
                            IFExDbConfig dbConfig,
                            IAsyncInitializable[] dependencies)
        : base(scopeProvider, resilientTransaction, dbConfig, dependencies)
    {
    }

    public async Task<IQueryable<T>> GetEntitiesAsync<T>(Expression<Func<TDbContext, IQueryable<T>>> query)
        where T : class => await RunFuncInDbContextAsync(dbContext => InternalGetEntities(dbContext, query));

    public async Task<EntityEntry<T>> AddAsync<T>(T entity,
                                                  string errorMessage = null,
                                                  bool saveChanges = true,
                                                  bool useTransaction = true) where T : class => await RunFuncInDbContextAsync(dbContext => dbContext.Add(entity),
            errorMessage,
            saveChanges,
            useTransaction);

    public async Task<EntityEntry<T>> UpdateAsync<T>(T entity,
                                                     string errorMessage = null,
                                                     bool saveChanges = true,
                                                     bool useTransaction = true) where T : class => await RunFuncInDbContextAsync(dbContext => dbContext.Update(entity),
            errorMessage,
            saveChanges,
            useTransaction);

    public async Task<EntityEntry<T>> RemoveAsync<T>(T entity,
                                                     string errorMessage = null,
                                                     bool saveChanges = true,
                                                     bool useTransaction = true) where T : class => await RunFuncInDbContextAsync(dbContext => dbContext.Remove(entity),
            errorMessage,
            saveChanges,
            useTransaction);

    public async Task AddRangeAsync<T>(IEnumerable<T> entities,
                                       string errorMessage = null,
                                       bool saveChanges = true,
                                       bool useTransaction = true) where T : class => await RunActionInDbContextAsync(dbContext => dbContext.AddRange(entities),
            errorMessage,
            saveChanges,
            useTransaction);

    protected IQueryable<T> InternalGetEntities<T>(TDbContext dbContext,
                                                   Expression<Func<TDbContext, IQueryable<T>>> query) where T : class =>
        query is not null
            ? query.Compile()(dbContext)
            : throw new InvalidOperationException("There is no provided query to be executed");
}