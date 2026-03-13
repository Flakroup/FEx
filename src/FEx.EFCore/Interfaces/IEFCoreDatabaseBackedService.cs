using Microsoft.EntityFrameworkCore;

namespace FEx.EFCore.Interfaces;

public interface IEFCoreDatabaseBackedService<TDbContext> : IDbServiceBase<TDbContext> where TDbContext : DbContext
{
    string DbKey { get; }
}