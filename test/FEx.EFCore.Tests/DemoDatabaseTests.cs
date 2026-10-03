using FEx.Samples.Shared;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FEx.EFCore.Tests;

public sealed class DemoDatabaseTests : IDisposable
{
    private readonly DirectoryInfo _appData = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), "fex-demo-db-" + Guid.NewGuid().ToString("N")));

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        _appData.Delete(true);
    }

    [Fact]
    public async Task First_run_creates_the_file_seeds_the_rows_and_loads_them()
    {
        var file = DemoDatabase.GetDatabaseFile(_appData);
        file.Exists.ShouldBeFalse();

        using var db = DemoDbContext.Create(file);
        var items = await DemoDatabase.SeedAndLoadAsync(db);

        file.Refresh();
        file.Exists.ShouldBeTrue();
        file.DirectoryName.ShouldBe(_appData.FullName);
        items.Count.ShouldBe(3);
        items.Select(item => item.Name).ShouldBe(["Async startup", "Startup window", "EF Core + SQLite"]);
    }

    [Fact]
    public async Task Second_run_loads_the_existing_rows_without_seeding_again()
    {
        var file = DemoDatabase.GetDatabaseFile(_appData);

        using (var first = DemoDbContext.Create(file))
            await DemoDatabase.SeedAndLoadAsync(first);

        using var second = DemoDbContext.Create(file);
        var items = await DemoDatabase.SeedAndLoadAsync(second);

        items.Count.ShouldBe(3);
        items.Select(item => item.Id).Distinct().Count().ShouldBe(3);
    }

    [Fact]
    public void Database_file_lives_in_the_app_data_folder_which_is_created_when_missing()
    {
        var nested = new DirectoryInfo(Path.Combine(_appData.FullName, "nested", "data"));

        var file = DemoDatabase.GetDatabaseFile(nested);

        nested.Exists.ShouldBeTrue();
        file.FullName.ShouldBe(Path.Combine(nested.FullName, DemoDatabase.FileName));
    }
}
