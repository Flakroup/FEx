using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FEx.EFCore.Interfaces;

public interface IDbServiceBase<TDbContext> : IPooledDbService<TDbContext> where TDbContext : DbContext
{
    Task<EntityEntry<T>> AddAsync<T>(T entity,
                                     string? errorMessage = null,
                                     bool saveChanges = true,
                                     bool useTransaction = true) where T : class;

    Task AddRangeAsync<T>(IEnumerable<T> entities,
                          string? errorMessage = null,
                          bool saveChanges = true,
                          bool useTransaction = true) where T : class;

    Task<IQueryable<T>> GetEntitiesAsync<T>(Expression<Func<TDbContext, IQueryable<T>>> query) where T : class;

    Task<EntityEntry<T>> RemoveAsync<T>(T entity,
                                        string? errorMessage = null,
                                        bool saveChanges = true,
                                        bool useTransaction = true) where T : class;

    Task<EntityEntry<T>> UpdateAsync<T>(T entity,
                                        string? errorMessage = null,
                                        bool saveChanges = true,
                                        bool useTransaction = true) where T : class;
}