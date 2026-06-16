using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Collections.Lists;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Core.Abstractions;
using FEx.Legacy.Asyncx.Enums;
using FEx.Legacy.Mvvm.Abstractions.Interfaces;
using FEx.WPFx.Extensions;
using JetBrains.Annotations;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FEx.WPFx.Controls;

public class FExUserControl : UserControl, IFExNotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    public ConcurrentDictionary<string, IDisposable> Subscriptions { get; }

    public bool IsInDesignMode { get; }

    public FExUserControl()
    {
        Subscriptions = new();
        IsInDesignMode = DesignerProperties.GetIsInDesignMode(this);

        if (IsInDesignMode)
            Background = SystemColors.ControlBrush;
    }

    public void OnPropertiesChanged(params string[] propertyNames)
    {
        if (propertyNames.IsNullOrEmptyList())
            return;

        foreach (var propertyName in propertyNames)
            OnPropertyChanged(propertyName);
    }

    public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (PropertyChanged is null
            || propertyName is null)
            return;

        FExCoreStatics.Dispatcher.InvokeOnMainThread(EventDelegate, this);

        return;

        void EventDelegate() => NotifyChanged(propertyName);
    }

    public virtual bool SetProperty<TRet>(ref TRet backingField,
                                          TRet newValue,
                                          Action<TRet> onPropertyChanged = null,
                                          [CallerMemberName] string propertyName = null)
    {
        if (EqualityHelper.IsEqual(ref backingField, newValue))
            return false;

        var oldValue = backingField;
        backingField = newValue;
        OnPropertySet(oldValue, newValue, propertyName);
        OnPropertyChanged(propertyName);
        onPropertyChanged?.Invoke(newValue);

        return true;
    }

    public virtual void OnPropertySet<T>(T oldValue, T newValue, string propertyName)
    {
    }

    public virtual async Task RunAsync<T>(T viewModel, Action action, object sender = null, JobSpecs? specs = null)
        where T : IThreadingAwareViewModel =>
        await viewModel.RunAsync(action, specs, () => PreAction(sender), isSuccess => PostAction(sender, isSuccess));

    public virtual async Task RunTaskAsync<T>(T viewModel,
                                              Func<Task> function,
                                              object sender = null,
                                              JobSpecs? specs = null) where T : IThreadingAwareViewModel =>
        await viewModel.RunTaskAsync(function,
            specs,
            () => PreAction(sender),
            isSuccess => PostAction(sender, isSuccess));

    public virtual async Task<TResult> RunTaskAsync<TResult, T>(T viewModel,
                                                                Func<Task<TResult>> function,
                                                                object sender = null,
                                                                JobSpecs? specs = null)
        where T : IThreadingAwareViewModel =>
        await viewModel.RunTaskAsync(function,
            specs,
            () => PreAction(sender),
            isSuccess => PostAction(sender, isSuccess));

    public virtual async Task<TResult> RunFuncAsync<TResult, T>(T viewModel,
                                                                Func<TResult> function,
                                                                object sender = null,
                                                                JobSpecs? specs = null)
        where T : IThreadingAwareViewModel =>
        await viewModel.RunFuncAsync(function,
            specs,
            () => PreAction(sender),
            isSuccess => PostAction(sender, isSuccess));

    protected virtual void PreAction(object sender) => DisableUIElement(sender);

    protected virtual void PostAction(object sender, bool isSuccess) => EnableUIElement(sender);

    private static void EnableUIElement(object sender)
    {
        if (sender is UIElement uiElement)
            uiElement.EnableUIElement(true);
    }

    private static void DisableUIElement(object sender)
    {
        if (sender is UIElement uiElement)
            uiElement.DisableUIElement(true);
    }

    [NotifyPropertyChangedInvocator]
    private void NotifyChanged([CallerMemberName] string propertyName = null)
    {
        if (propertyName is null
            || PropertyChanged is null)
            return;

        PropertyChanged.HandlePropertyChanged(this, propertyName);
    }
}