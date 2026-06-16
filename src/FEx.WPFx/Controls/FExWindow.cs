using FEx.Agnostics.Abstractions.Extensions;
using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Extensions;
using FEx.Legacy.Asyncx.Enums;
using FEx.Legacy.Mvvm.Abstractions.Interfaces;
using FEx.MVVM;
using FEx.WPFx.Extensions;
using FEx.WPFx.ViewModels;
using ReactiveUI;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;

namespace FEx.WPFx.Controls;

public class FExWindow<TViewModel> : Window, IViewFor<TViewModel>, IRunAsyncView
    where TViewModel : WpfProgressListenerViewModel
{
    public static DependencyProperty ViewModelProperty { get; } = DependencyProperty.Register(nameof(ViewModel),
        typeof(TViewModel),
        typeof(FExWindow<TViewModel>));

    public bool IsInDesignMode { get; }
    public Grid MainGrid { get; protected set; }
    public Screen DisplayScreen { get; protected set; }

    public TViewModel ViewModel
    {
        get => (TViewModel)FExCoreStatics.Dispatcher.InvokeOnMainThread(() => GetValue(ViewModelProperty));
        set => FExCoreStatics.Dispatcher.InvokeOnMainThread(() => SetValue(ViewModelProperty, value));
    }

    protected double FixedWidth { get; set; }

    object IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (TViewModel)value;
    }

    public FExWindow()
    {
        IsInDesignMode = DesignerProperties.GetIsInDesignMode(this);

        if (!IsInDesignMode)
        {
            DataContextChanged += FExWindow_DataContextChanged;
            Loaded += OnLoaded;
        }
    }

    public virtual async Task RunAsync(Action action, object sender = null, JobSpecs? specs = null) =>
        await ViewModel.RunAsync(action, specs, () => PreAction(sender), isSuccess => PostAction(sender, isSuccess));

    public virtual async Task RunTaskAsync(Func<Task> function, object sender = null, JobSpecs? specs = null) =>
        await ViewModel.RunTaskAsync(function,
            specs,
            () => PreAction(sender),
            isSuccess => PostAction(sender, isSuccess));

    public virtual async Task<TResult> RunTaskAsync<TResult>(Func<Task<TResult>> function,
                                                             object sender = null,
                                                             JobSpecs? specs = null) =>
        await ViewModel.RunTaskAsync(function,
            specs,
            () => PreAction(sender),
            isSuccess => PostAction(sender, isSuccess));

    public virtual async Task<TResult> RunFuncAsync<TResult>(Func<TResult> function,
                                                             object sender = null,
                                                             JobSpecs? specs = null) =>
        await ViewModel.RunFuncAsync(function,
            specs,
            () => PreAction(sender),
            isSuccess => PostAction(sender, isSuccess));

    public void FExWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) =>
        RefreshViewModel(true);

    protected virtual void OnLoaded(object sender, RoutedEventArgs e)
    {
        FExWpfx.WindowLoaded?.Invoke(sender, e);
        this.PlaceToPrimaryMonitor();
    }

    protected virtual void PreAction(object sender) => DisableUIElement(sender);

    protected virtual void PostAction(object sender, bool isSuccess) => EnableUIElement(sender);

    protected virtual void ReScale()
    {
        try
        {
            var height = DisplayScreen.WorkingArea.Height / (double)DisplayScreen.WorkingArea.Width * FixedWidth;
            var w = ActualWidth / FixedWidth;
            var h = ActualHeight / height;

            if (MainGrid.LayoutTransform is not ScaleTransform scaler)
            {
                MainGrid.LayoutTransform = new ScaleTransform(w, h);
            }
            else
            {
                if (scaler.HasAnimatedProperties)
                {
                    scaler.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    scaler.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                }

                scaler.ScaleX = w;
                scaler.ScaleY = h;
                MainGrid.LayoutTransform = scaler;
            }
        }
        catch (Exception ex)
        {
            ex.HandleException(false);
        }
    }

    protected void RefreshViewModel(bool refresh = false)
    {
        if (DataContext is not TViewModel)
            throw new($"{nameof(DataContext)} must inherit {nameof(TViewModel)}");

        ViewModel?.View = null;

        if (ViewModel is null || refresh)
            ViewModel = this.GetViewModel<TViewModel>();

        ViewModel!.View = this;
    }

    protected void EnableRescaling(Grid mainGrid, double fixedWidth = 1920D)
    {
        MainGrid = mainGrid;
        DisplayScreen = this.GetWindowsScreen();
        FixedWidth = fixedWidth;

        ViewModel!.Subscriptions.ReplaceAndDisposeOldValue(nameof(MainGrid),
            () => Observable
                .FromEventPattern<SizeChangedEventHandler,
                    SizeChangedEventArgs>(h => SizeChanged += h, h => SizeChanged -= h)
                .Throttle(FExMvvm.DefaultUIRefreshInterval)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(_ => ReScale()));
    }

    private static void EnableUIElement(object sender)
    {
        if (sender is not UIElement element)
            return;

        element.EnableUIElement(true);
    }

    private static void DisableUIElement(object sender)
    {
        if (sender is not UIElement element)
            return;

        element.DisableUIElement(true);
    }
}