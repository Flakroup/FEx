using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace FEx.WPFx.Fluent.Views;

/// <summary>"Wczytano o 14:05" and the refresh button beside it, shared by every tab that loads on activation.</summary>
public partial class LoadIndicator : UserControl
{
    /// <summary>Where the data comes from, as the app names it; the refresh button's tooltip is built from it.</summary>
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(string), typeof(LoadIndicator),
            new PropertyMetadata(string.Empty, (d, _) => ((LoadIndicator)d).ApplySource()));

    public LoadIndicator()
    {
        InitializeComponent();
        ApplySource();
    }

    /// <summary>The name of the data source, in the form that fits "dane z {Source}".</summary>
    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value ?? string.Empty);
    }

    private void ApplySource()
    {
        var text = LoadText.Refresh(Source);
        btnRefresh.ToolTip = text;
        AutomationProperties.SetName(btnRefresh, text);
    }

    /// <summary>The operator asked to read the data again.</summary>
    public event EventHandler? RefreshRequested;

    /// <summary>Shows when the data was read; empty while there is none (loading, failed).</summary>
    public void Show(DateTime? loadedAt) => lblFresh.Text = loadedAt is { } at ? LoadText.Fresh(at) : string.Empty;

    private void BtnRefresh_Click(object sender, RoutedEventArgs e) => RefreshRequested?.Invoke(this, EventArgs.Empty);
}
