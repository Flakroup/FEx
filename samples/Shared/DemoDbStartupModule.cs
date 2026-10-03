// ReSharper disable RedundantUsingDirective - needed where implicit usings are off (Avalonia sample, EFCore tests)
using FEx.DependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.Samples.Shared;

/// <summary>Holds the rows the startup module loaded, for the main window's view model.</summary>
public sealed class DemoDataStore
{
    public IReadOnlyList<DemoItem> Items { get; private set; } = [];

    public void Set(IReadOnlyList<DemoItem> items) => Items = items;
}

/// <summary>
/// Reads the demo database while the container is built, so a sample launch exercises real async work (database I/O)
/// during startup: the startup window is shown and the UI stays responsive meanwhile.
/// </summary>
public sealed class DemoDbStartupModule(DemoDbContext db, DemoDataStore store) : InitializeOnlyModule
{
    public override async ValueTask OnCompleteInitializationAsync(IServiceCollection services) =>
        store.Set(await DemoDatabase.SeedAndLoadAsync(db));
}
