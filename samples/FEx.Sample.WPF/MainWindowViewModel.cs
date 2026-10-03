using FEx.Samples.Shared;

namespace FEx.Sample.WPF;

/// <summary>View model of the main window: the rows the startup module loaded from the demo database.</summary>
public sealed class MainWindowViewModel(DemoDataStore store)
{
    public IReadOnlyList<DemoItem> Items { get; } = store.Items;

    public string Summary => $"{Items.Count} rows loaded from the demo database during startup";
}
