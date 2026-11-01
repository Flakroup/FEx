using System;
using System.Windows;
using System.Windows.Controls;

namespace FEx.WPFx.Fluent.Views;

/// <summary>
/// What a tab shows in place of its grid when there are no rows: the skeleton while the data is read, the empty
/// list, or the reason the database could not be asked. The rows themselves are the tab's own business - this
/// control hides itself once there are some.
/// </summary>
public partial class LoadOverlay : UserControl
{
    /// <summary>Where the data comes from, as the app names it; the sentences that mention it are built from it.</summary>
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(string), typeof(LoadOverlay),
            new PropertyMetadata(string.Empty, (d, _) => ((LoadOverlay)d).ApplySource()));

    /// <summary>What the app connects to, in the form that follows "połączyć z" ("bazą Płatnika"); apart from <see cref="Source"/> because Polish declines the two differently.</summary>
    public static readonly DependencyProperty ConnectionTargetProperty =
        DependencyProperty.Register(nameof(ConnectionTarget), typeof(string), typeof(LoadOverlay),
            new PropertyMetadata(string.Empty, (d, _) => ((LoadOverlay)d).ApplySource()));

    private LoadState _state = LoadState.Idle;

    public LoadOverlay()
    {
        InitializeComponent();
        btnRetry.Content = LoadText.Retry;
        btnSettings.Content = LoadText.OpenSettings;
        ApplySource();
    }

    /// <summary>The name of the data source, in the form that fits "danych z {Source}".</summary>
    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value ?? string.Empty);
    }

    /// <summary>The name of what the app connects to, in the form that fits "połączyć z {ConnectionTarget}".</summary>
    public string ConnectionTarget
    {
        get => (string)GetValue(ConnectionTargetProperty);
        set => SetValue(ConnectionTargetProperty, value ?? string.Empty);
    }

    private void ApplySource()
    {
        lblLoading.Text = LoadText.Loading(Source);
        if (_state is LoadState.Failed)
            lblTitle.Text = LoadText.ErrorTitle(ConnectionTarget);
    }

    /// <summary>The operator asked to read the data again.</summary>
    public event EventHandler? RetryRequested;

    /// <summary>The operator asked for the settings, where the connection is fixed.</summary>
    public event EventHandler? OpenSettingsRequested;

    /// <param name="state">The load state to draw.</param>
    /// <param name="emptyTitle">The bold line of the empty list - each tab's own sentence.</param>
    /// <param name="emptyHint">The muted line under it; null where the tab has no year or period to change.</param>
    public void Show(LoadState state, string emptyTitle, string? emptyHint)
    {
        _state = state;
        Visibility = state is LoadState.Loaded ? Visibility.Collapsed : Visibility.Visible;
        var skeleton = state is LoadState.Idle or LoadState.Loading;
        pnlLoading.Visibility = skeleton ? Visibility.Visible : Visibility.Collapsed;
        pnlMessage.Visibility = skeleton || state is LoadState.Loaded ? Visibility.Collapsed : Visibility.Visible;

        if (state is LoadState.Empty)
            Message(emptyTitle, emptyHint, danger: false, retry: false, settings: false);
        else if (state is LoadState.Failed)
            Message(LoadText.ErrorTitle(ConnectionTarget), LoadText.ErrorHint, danger: true, retry: true, settings: true);
        else if (state is LoadState.Missing)
            Message(LoadText.MissingTitle, LoadText.MissingHint, danger: false, retry: false, settings: true);
    }

    /// <summary>The "nothing configured" state with a tab's own sentences and the button to Ustawienia.</summary>
    public void ShowMissing(string title, string? hint)
    {
        _state = LoadState.Missing;
        Visibility = Visibility.Visible;
        pnlLoading.Visibility = Visibility.Collapsed;
        pnlMessage.Visibility = Visibility.Visible;
        Message(title, hint, danger: false, retry: false, settings: true);
    }

    private void Message(string title, string? hint, bool danger, bool retry, bool settings)
    {
        lblTitle.Text = title;
        lblTitle.SetResourceReference(TextBlock.ForegroundProperty, danger ? "Danger" : "Ink");
        lblHint.Text = hint ?? string.Empty;
        lblHint.Visibility = string.IsNullOrEmpty(hint) ? Visibility.Collapsed : Visibility.Visible;
        btnRetry.Visibility = retry ? Visibility.Visible : Visibility.Collapsed;
        btnSettings.Visibility = settings ? Visibility.Visible : Visibility.Collapsed;
        pnlButtons.Visibility = retry || settings ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnRetry_Click(object sender, RoutedEventArgs e) => RetryRequested?.Invoke(this, EventArgs.Empty);

    private void BtnSettings_Click(object sender, RoutedEventArgs e) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);
}
