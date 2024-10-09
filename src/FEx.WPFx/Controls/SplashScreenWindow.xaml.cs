using FEx.Abstractions.Interfaces;
using FEx.Basics.Extensions;
using FEx.Common.Abstractions.Interfaces;
using FEx.Common.Extensions;
using FEx.Extensions;
using FEx.WPFx.Abstractions.Interfaces;
using FEx.WPFx.Extensions;
using FEx.WPFx.Helpers;
using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;

namespace FEx.WPFx.Controls;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class SplashScreenWindow : Window, INotifyPropertyChanged
{
    public static readonly DependencyProperty DesiredWidthProperty =
        DependencyPropertyExtensions.CreateDependencyProperty<SplashScreenWindow, double>(view => view.DesiredWidth,
            propertyChanged: tuple => OnWidthChanged(tuple.view, tuple.newValue));

    public static readonly DependencyProperty DesiredHeightProperty =
        DependencyPropertyExtensions.CreateDependencyProperty<SplashScreenWindow, double>(view => view.DesiredHeight,
            propertyChanged: tuple => OnHeightChanged(tuple.view, tuple.newValue));

    public static readonly DependencyProperty TotalDesiredWidthProperty =
        DependencyPropertyExtensions.CreateDependencyProperty<SplashScreenWindow, double>(
            view => view.TotalDesiredWidth,
            propertyChanged: tuple => OnWidthChanged(tuple.view, tuple.newValue));

    public static readonly DependencyProperty TotalDesiredHeightProperty =
        DependencyPropertyExtensions.CreateDependencyProperty<SplashScreenWindow, double>(
            view => view.TotalDesiredHeight,
            propertyChanged: tuple => OnHeightChanged(tuple.view, tuple.newValue));

    public static EventHandler<EventArgs> CloseIt;

    private readonly IAppInfoProvider _appInfoProvider;
    private readonly IAsyncHelper _asyncHelper;
    private readonly IStatusService _statusService;
    private readonly IAppConfig _appConfig;

    private string _status;
    private double _desiredTextWidth;

    public event PropertyChangedEventHandler PropertyChanged;

    public static Task<bool> InitializationTask { get; private set; }

    public string AppNameAndVersion => _appInfoProvider.NameLineVersion;

    public BitmapImage ApplicationLogo { get; private set; }

    public FontFamily Font { get; }

    public string SplashText { get; }

    public Brush ForegroundBrush { get; }

    public string ResourceName { get; protected set; }

    public double DesiredTextWidth
    {
        get => _desiredTextWidth;
        set => SetProperty(ref _desiredTextWidth, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public double DesiredWidth
    {
        get => (double)GetValue(DesiredWidthProperty);
        set => SetValue(DesiredWidthProperty, value);
    }

    public double DesiredHeight
    {
        get => (double)GetValue(DesiredHeightProperty);
        set => SetValue(DesiredHeightProperty, value);
    }

    public double TotalDesiredWidth
    {
        get => (double)GetValue(TotalDesiredWidthProperty);
        set => SetValue(TotalDesiredWidthProperty, value);
    }

    public double TotalDesiredHeight
    {
        get => (double)GetValue(TotalDesiredHeightProperty);
        set => SetValue(TotalDesiredHeightProperty, value);
    }

    protected Assembly ResourceAssembly { get; }

    protected IStatusHub StatusHub { get; set; }

    protected Guid? MainHubKey { get; }

    public SplashScreenWindow(IAppInfoProvider appInfoProvider,
                              IAsyncHelper asyncHelper,
                              IStatusService statusService,
                              IAppConfig appConfig)
    {
        _appInfoProvider = appInfoProvider;
        _asyncHelper = asyncHelper;
        _statusService = statusService;
        _appConfig = appConfig;
        _appConfig.SplashDesign.Initialize();

        MainHubKey = _statusService.MainHubKey;
        ResourceAssembly = _appInfoProvider.EntryAssembly;
        ResourceName = _appConfig.SplashResourceName;
        Font = _appConfig.SplashDesign.FontFamily;
        SplashText = _appInfoProvider.NameLineVersionWithPrefix;
        Loaded += OnSplashLoaded;
        CloseIt += CloseSplash;
        InitializationTask = _asyncHelper.FireAndForget(Initialize).Task;
        ForegroundBrush = _appConfig.SplashDesign.Foreground ?? Brushes.DodgerBlue;
        DesiredWidth = _appConfig.SplashSize?.Width ?? 256D;
        DesiredHeight = _appConfig.SplashSize?.Height ?? 256D;
        DesiredTextWidth = DesiredWidth;
        TotalDesiredHeight = DesiredWidth + 44;
        TotalDesiredWidth = DesiredWidth + 44;
        InitializeComponent();
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (propertyName is null
            || PropertyChanged is null)
            return;

        PropertyChanged.HandlePropertyChanged(this, propertyName);
    }

    protected bool SetProperty<T>(ref T backingField,
                                  T newValue,
                                  Action<T> onPropertyChanged = null,
                                  [CallerMemberName] string propertyName = null) =>
        this.SetPropertyStatic(ref backingField,
            newValue,
            p => WhenPropertyChanged(p, newValue, onPropertyChanged),
            propertyName);

    private static void OnWidthChanged(SplashScreenWindow window, double newValue)
    {
        window.MaxWidth = newValue;
        window.Width = newValue;
    }

    private static void OnHeightChanged(SplashScreenWindow window, double newValue)
    {
        window.MaxHeight = newValue;
        window.Height = newValue;
    }

    private static UnmanagedMemoryStream GetResourceStream(ResourceManager resourceManager, string resourceName) =>
        resourceManager.GetStream(resourceName, CultureInfo.CurrentUICulture)
        ?? resourceManager.GetStream(ResourceIdHelper.GetResourceIdFromRelativePath(resourceName),
            CultureInfo.CurrentUICulture);

    private void WhenPropertyChanged<T>(string propertyName, T newValue, Action<T> onPropertyChanged)
    {
        OnPropertyChanged(propertyName);
        onPropertyChanged?.Invoke(newValue);
    }

    private bool Initialize()
    {
        try
        {
            if (MainHubKey.HasValue)
            {
                StatusHub = _statusService.GetOrAdd(MainHubKey.Value);
                OnStatusChange();
                StatusHub.AttachToStatusChanges((_, _) => OnStatusChange(), (_, _) => OnStatusChange(), OnStatusChange);
            }

            SetLogo();

            return true;
        }
        catch (Exception ex)
        {
            ex.HandleException(false);

            return false;
        }
    }

    private void SetLogo()
    {
        ResourceName = ResourceName.Guard(nameof(ResourceName)).ToLowerInvariant();

        var resourceManager =
            new ResourceManager($"{new AssemblyName(ResourceAssembly.FullName).Name}.g", ResourceAssembly);

        using UnmanagedMemoryStream stream = GetResourceStream(resourceManager, ResourceName).Guard(nameof(stream));
        ApplicationLogo = stream.ToBitmapImage(true);
    }

    private void OnSplashLoaded(object sender, RoutedEventArgs e)
    {
        this.PlaceToPrimaryMonitor();
        DesiredTextWidth = DesiredWidth - progressCircle.ActualWidth - 5;
    }

    private void CloseSplash(object sender, EventArgs e) => Dispatcher?.Invoke(Close);

    [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods")]
    private async void OnStatusChange() =>
        await Dispatcher.InvokeAsync(() => Status = StatusHub.GetStatusString(Environment.NewLine)?.ToUpper(),
            DispatcherPriority.Send);

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Environment.Exit(0);
}