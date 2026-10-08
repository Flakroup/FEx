using FEx.Imaging.Windows.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shouldly;
using System.Linq;
using Xunit;

namespace FEx.Imaging.Windows.Tests;

/// <summary>The shipped migrations against the current <see cref="FilesCacheContext" /> model, on a real SQLite database.</summary>
public sealed class FilesCacheContextMigrationsTests : ImagingTestBase
{
    private const string InitialCreate = "20191014120352_InitialCreate";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public FilesCacheContextMigrationsTests() => _connection.Open();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _connection.Dispose();

        base.Dispose(disposing);
    }

    private FilesCacheContext CreateContext() =>
        new(new DbContextOptionsBuilder<FilesCacheContext>().UseSqlite(_connection).Options);

    [Fact]
    public void Context_WithoutOptions_UsesTheDefaultCacheDatabase()
    {
        using var context = new FilesCacheContext(new DbContextOptions<FilesCacheContext>());

        var options = RelationalOptionsExtension.Extract(context.GetService<IDbContextOptions>());

        options.ConnectionString.ShouldBe(@"data source=C:\ProgramData\Flakroup\ImageCache\IndexEF.db");
    }

    [Fact]
    public void Migrations_HaveNoPendingModelChanges()
    {
        using var context = CreateContext();

#if NET9_0_OR_GREATER
        context.Database.HasPendingModelChanges().ShouldBeFalse();
#else
        var snapshot = context.GetService<IMigrationsAssembly>().ModelSnapshot.ShouldNotBeNull().Model;
        context.GetService<IMigrationsModelDiffer>().HasDifferences(snapshot, context.Model).ShouldBeFalse();
#endif
    }

    [Fact]
    public void Migrate_DatabaseFromTheFirstMigration_KeepsItsRows()
    {
        using (var legacy = CreateContext())
        {
            legacy.GetService<IMigrator>().Migrate(InitialCreate);
            legacy.Database.ExecuteSqlRaw(
                "INSERT INTO IndexEntries (AbsoluteUri, CheckSum, FilePath, ResponseContentLength) " +
                "VALUES ('https://images.test/photos/cat.png', 'abc', 'C:\\cache\\cat.png', 42), " +
                "('https://images.test/photos/dog.png', NULL, NULL, -1)");
        }

        using (var upgrade = CreateContext())
            upgrade.Database.Migrate();

        using var context = CreateContext();
        context.Database.GetPendingMigrations().ShouldBeEmpty();
        var rows = context.IndexEntries.AsNoTracking()
            .OrderBy(x => x.AbsoluteUri)
            .Select(x => new
            {
                x.AbsoluteUri,
                x.CheckSum,
                x.FilePath,
                x.ResponseContentLength,
                x.PixelHeight,
                x.PixelWidth
            })
            .ToList();

        rows.Count.ShouldBe(2);
        rows[0].AbsoluteUri.ShouldBe("https://images.test/photos/cat.png");
        rows[0].CheckSum.ShouldBe("abc");
        rows[0].FilePath.ShouldBe(@"C:\cache\cat.png");
        rows[0].ResponseContentLength.ShouldBe(42);
        rows[0].PixelHeight.ShouldBe(0);
        rows[0].PixelWidth.ShouldBe(0);
        rows[1].AbsoluteUri.ShouldBe("https://images.test/photos/dog.png");
        rows[1].CheckSum.ShouldBeNull();
        rows[1].FilePath.ShouldBeNull();
        rows[1].ResponseContentLength.ShouldBe(-1);
    }

    [Fact]
    public void SaveChanges_EntryWithoutAFile_StoresNullPathAndCheckSum()
    {
        using (var migrate = CreateContext())
            migrate.Database.Migrate();

        using (var writer = CreateContext())
        using (var entry = new IndexEntry { AbsoluteUri = ImageUrl.AbsoluteUri })
        {
            writer.IndexEntries.Add(entry);
            writer.SaveChanges();
        }

        using var context = CreateContext();
        var row = context.IndexEntries.AsNoTracking()
            .Select(x => new
            {
                x.AbsoluteUri,
                x.CheckSum,
                x.FilePath
            })
            .Single();

        row.AbsoluteUri.ShouldBe(ImageUrl.AbsoluteUri);
        row.CheckSum.ShouldBeNull();
        row.FilePath.ShouldBeNull();
    }
}
