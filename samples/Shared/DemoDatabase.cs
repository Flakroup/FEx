// ReSharper disable RedundantUsingDirective - needed where implicit usings are off (Avalonia sample, EFCore tests)
using FEx.EFCore.Configuration;
using FEx.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.Samples.Shared;

/// <summary>A row of the demo database.</summary>
public sealed class DemoItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>The demo database: a small SQLite file under the app data folder.</summary>
public sealed class DemoDbContext(DbContextOptions<DemoDbContext> options) : DbContext(options)
{
    public DbSet<DemoItem> Items => Set<DemoItem>();

    /// <summary>Builds a context over the given SQLite file with the FEx.EFCore SQLite helper.</summary>
    public static DemoDbContext Create(FileInfo databaseFile)
    {
        var config = new FExDbConfig { UseSqlite = true, SqliteDbFile = databaseFile };
        var options = new DbContextOptionsBuilder<DemoDbContext>();
        options.UseSqlite(config);

        return new DemoDbContext(options.Options);
    }
}

/// <summary>Creates, seeds and loads the demo database; shared by both samples.</summary>
public static class DemoDatabase
{
    public const string FileName = "FExSampleDemo.db";

    /// <summary>The database file inside the app data folder (the folder is created if it is missing).</summary>
    public static FileInfo GetDatabaseFile(DirectoryInfo appData)
    {
        Directory.CreateDirectory(appData.FullName);

        return new FileInfo(Path.Combine(appData.FullName, FileName));
    }

    /// <summary>Creates the database if needed, seeds a few rows on the first run, and loads all rows.</summary>
    public static async Task<IReadOnlyList<DemoItem>> SeedAndLoadAsync(DemoDbContext db)
    {
        // Written the way a consumer would, without ConfigureAwait(false): the async-DB-during-bootstrap pattern issue #84
        // was filed about (the old blocking constructor); startup now awaits it.
        await db.Database.EnsureCreatedAsync();

        if (!await db.Items.AnyAsync())
        {
            await db.Items.AddRangeAsync(
                new DemoItem { Name = "Async startup", Description = "The container build awaits this database." },
                new DemoItem { Name = "Startup window", Description = "Shown while the rows are loaded." },
                new DemoItem { Name = "EF Core + SQLite", Description = "A file under the app data folder." });

            await db.SaveChangesAsync();
        }

        return await db.Items.OrderBy(item => item.Id).ToListAsync();
    }
}
