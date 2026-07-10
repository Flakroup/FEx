using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Windows;

namespace FEx.WPFx.Extensions;

#pragma warning disable S2360 // Factory methods with callback-style optional parameters - overloads impractical

public static class DependencyPropertyExtensions
{
    public static DependencyProperty CreateTwoWayDependencyProperty<TView, TProperty>(
        Expression<Func<TView, TProperty>> propertyExpression,
        TProperty defaultValue = default!,
        Action<(TView view, TProperty oldValue, TProperty newValue)>? propertyChanged = null,
        Func<(TView view, TProperty currentValue), TProperty>? coerceValue = null,
        Func<TProperty, bool>? validateValue = null) where TView : DependencyObject =>
        CreateDependencyProperty(propertyExpression,
            defaultValue,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            propertyChanged,
            coerceValue,
            validateValue);

    public static DependencyProperty CreateTwoWayDependencyProperty<TView, TProperty>(string propertyName,
        TProperty defaultValue = default!,
        Action<(TView view, TProperty oldValue, TProperty newValue)>? propertyChanged = null,
        Func<(TView view, TProperty currentValue), TProperty>? coerceValue = null,
        Func<TProperty, bool>? validateValue = null) where TView : DependencyObject =>
        CreateDependencyProperty(propertyName,
            defaultValue,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            propertyChanged,
            coerceValue,
            validateValue);

    public static DependencyProperty CreateDependencyProperty<TView, TProperty>(
        Expression<Func<TView, TProperty>> propertyExpression,
        TProperty defaultValue = default!,
        FrameworkPropertyMetadataOptions flags = FrameworkPropertyMetadataOptions.None,
        Action<(TView view, TProperty oldValue, TProperty newValue)>? propertyChanged = null,
        Func<(TView view, TProperty currentValue), TProperty>? coerceValue = null,
        Func<TProperty, bool>? validateValue = null) where TView : DependencyObject
    {
        if (propertyExpression.Body is not MemberExpression memberExpression)
            throw new ArgumentException("Expression must be a member expression", nameof(propertyExpression));

        var propertyName = memberExpression.Member.Name;

        return CreateDependencyProperty(propertyName, defaultValue, flags, propertyChanged, coerceValue, validateValue);
    }

    public static DependencyProperty CreateDependencyProperty<TView, TProperty>(string propertyName,
        TProperty defaultValue = default!,
        FrameworkPropertyMetadataOptions flags = FrameworkPropertyMetadataOptions.None,
        Action<(TView view, TProperty oldValue, TProperty newValue)>? propertyChanged = null,
        Func<(TView view, TProperty currentValue), TProperty>? coerceValue = null,
        Func<TProperty, bool>? validateValue = null) where TView : DependencyObject =>
        DependencyProperty.Register(propertyName,
            typeof(TProperty),
            typeof(TView),
            GetPropertyMetadata(defaultValue, flags, propertyChanged, coerceValue),
            GetValidateValueCallback(validateValue));

    public static DependencyProperty CreateCollectionDependencyProperty<TView, TProperty>(string propertyName,
        TProperty defaultValue = default!,
        FrameworkPropertyMetadataOptions flags = FrameworkPropertyMetadataOptions.None,
        Action<(TView view, TProperty oldValue, TProperty newValue)>? propertyChanged = null,
        Action<(TView view, IList? oldItems, IList? newItems)>? collectionChanged = null,
        Func<(TView view, TProperty currentValue), TProperty>? coerceValue = null,
        Func<TProperty, bool>? validateValue = null) where TView : DependencyObject
        where TProperty : INotifyCollectionChanged
    {
        TView view;

        return CreateDependencyProperty(propertyName,
            defaultValue,
            flags,
            tuple =>
            {
                if (collectionChanged is not null)
                    AttachCollectionChangedCallback(tuple.view, tuple.oldValue, tuple.newValue);

                propertyChanged?.Invoke((tuple.view, tuple.oldValue, tuple.newValue));
            },
            coerceValue,
            validateValue);

        void AttachCollectionChangedCallback(TView bindable, TProperty oldValue, TProperty newValue)
        {
            view = bindable;

            if (oldValue is INotifyCollectionChanged c)
                c.CollectionChanged -= OnCollectionChanged;

            if (newValue is INotifyCollectionChanged c1)
                c1.CollectionChanged += OnCollectionChanged;
        }

        void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            collectionChanged?.Invoke((view, e.OldItems, e.NewItems));
        }
    }

    public static DependencyProperty CreateAttachedProperty<TView, TProperty>(string propertyName,
                                                                              TProperty defaultValue = default!,
                                                                              FrameworkPropertyMetadataOptions flags =
                                                                                  FrameworkPropertyMetadataOptions.None,
                                                                              Action<(TView view, TProperty oldValue,
                                                                                      TProperty newValue)>?
                                                                                  propertyChanged =
                                                                                  null,
                                                                              Func<(TView view, TProperty currentValue),
                                                                                  TProperty>? coerceValue = null,
                                                                              Func<TProperty, bool>? validateValue =
                                                                                  null)
        where TView : DependencyObject =>
        DependencyProperty.RegisterAttached(propertyName,
            typeof(TProperty),
            typeof(TView),
            GetPropertyMetadata(defaultValue, flags, propertyChanged, coerceValue),
            GetValidateValueCallback(validateValue));

    private static ValidateValueCallback? GetValidateValueCallback<TProperty>(Func<TProperty, bool>? validateValue) =>
        validateValue is null
            ? null
            : v => validateValue((TProperty)v);

    private static FrameworkPropertyMetadata GetPropertyMetadata<TView, TProperty>(TProperty defaultValue,
            FrameworkPropertyMetadataOptions flags,
            Action<(TView view, TProperty oldValue, TProperty newValue)>? propertyChanged,
            Func<(TView view, TProperty currentValue), TProperty>? coerceValue
        //, bool isAnimationProhibited,
        //UpdateSourceTrigger defaultUpdateSourceTrigger
    ) where TView : DependencyObject =>
        new(defaultValue,
            flags,
            propertyChanged is null
                ? null
                : (b, e) => propertyChanged(((TView)b, (TProperty)e.OldValue, (TProperty)e.NewValue)),
            coerceValue is null
                ? null
                : (b, v) => coerceValue(((TView)b, (TProperty)v)));
}

#pragma warning restore S2360
