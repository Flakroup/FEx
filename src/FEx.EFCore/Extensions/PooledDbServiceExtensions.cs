using FEx.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace FEx.EFCore.Extensions;

public static class PooledDbServiceExtensions
{
    public static Task RunActionInDbContextAsync<TDbContext>(this IPooledDbService<TDbContext> service,
                                                             Action<TDbContext> func)
        where TDbContext : DbContext =>
        service.RunActionInDbContextAsync(func, null, true, true);

    public static Task<T> RunFuncInDbContextAsync<TDbContext, T>(this IPooledDbService<TDbContext> service,
                                                                  Func<TDbContext, T> func)
        where TDbContext : DbContext =>
        service.RunFuncInDbContextAsync(func, null, true, true);

    public static Task RunTaskInDbContextAsync<TDbContext>(this IPooledDbService<TDbContext> service,
                                                           Func<TDbContext, Task> func)
        where TDbContext : DbContext =>
        service.RunTaskInDbContextAsync(func, null, true, true);

    public static Task<T> RunTaskInDbContextAsync<TDbContext, T>(this IPooledDbService<TDbContext> service,
                                                                  Func<TDbContext, Task<T>> func)
        where TDbContext : DbContext =>
        service.RunTaskInDbContextAsync(func, null, true, true);

    public static Task<T> RunTaskInDbContextAsync<TDbContext, T>(this IPooledDbService<TDbContext> service,
                                                                  Func<TDbContext, Func<Task<T>>> func)
        where TDbContext : DbContext =>
        service.RunTaskInDbContextAsync(func, null, true, true);
}
