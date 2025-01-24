using FEx.Common.Extensions;
using FEx.Extensions.Base.Helpers;
using FEx.Extensions.Collections.Enumerables;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FEx.Extensions;

/// <summary>
///     Object extensions class.
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    ///     Indicates that the specified reference is not a null reference
    /// </summary>
    /// <typeparam name="T">Current Type.</typeparam>
    /// <param name="value">Reference to be tested</param>
    /// <returns>True, if specified value is not a null reference; Otherwise False.</returns>
    [ContractAnnotation("null => false")]
    public static bool ReferenceIsNotNull<T>(this T value) => value is not null;

    /// <summary>
    ///     Execute a action if T Not null.
    /// </summary>
    /// <typeparam name="T">Current Type.</typeparam>
    /// <param name="value">Reference to be tested</param>
    /// <param name="action">Action to execute if value Not null.</param>
    public static void IfNotNull<T>(this T value, Action action)
    {
        if (value is not null)
            action();
    }

    /// <summary>
    ///     Execute a action if T Not null.
    /// </summary>
    /// <typeparam name="T">Current Type.</typeparam>
    /// <param name="value">Reference to be tested</param>
    /// <param name="action">Action to execute if value Not null.</param>
    public static void IfNotNull<T>(this T value, Action<T> action)
    {
        if (value is not null)
            action(value);
    }

    /// <summary>
    ///     Execute a func if T Not null.
    /// </summary>
    /// <typeparam name="T">Current Type.</typeparam>
    /// <typeparam name="TOut">Type for out parameter.</typeparam>
    /// <param name="value">Reference to be tested</param>
    /// <param name="fn">Func to execute if value Not null.</param>
    /// <returns>If value not null, return the result of the Func; Otherwise return Default(TOut).</returns>
    public static TOut IfNotNull<T, TOut>(this T value, Func<T, TOut> fn) =>
        value is not null
            ? fn(value)
            : default;

    /// <summary>
    ///     Execute a action if T isnull.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">Reference to be tested</param>
    /// <param name="action">Action to execute if value Not null.</param>
    public static void IfNull<T>(this T value, Action action)
    {
        if (value is null)
            action();
    }

    /// <summary>
    ///     Execute a action if T isnull.
    /// </summary>
    /// <typeparam name="T">Current Type.</typeparam>
    /// <param name="value">Reference to be tested</param>
    /// <param name="action">Action to execute if value Not null.</param>
    public static void IfNull<T>(this T value, Action<T> action)
    {
        if (value is null)
            action(default);
    }

    /// <summary>
    ///     Try to Dispose the current object.
    /// </summary>
    /// <typeparam name="T">Current type.</typeparam>
    /// <param name="value">Current instance.</param>
    public static void TryDispose<T>(this T value)
    {
        if (value is IDisposable disposable)
#pragma warning disable IDISP007
            disposable.Dispose();
#pragma warning restore IDISP007
    }

    /// <summary>
    ///     Indicates that the specified reference is a null reference
    /// </summary>
    /// <typeparam name="T">Current Type.</typeparam>
    /// <param name="value">Reference to be tested</param>
    /// <returns>True, if specified value is a null reference; Otherwise False.</returns>
    [ContractAnnotation("null => true")]
    public static bool ReferenceIsNull<T>(this T value) => value is null;

    /// <summary>
    ///     Execute a Action with TInput as parameter.
    /// </summary>
    /// <typeparam name="TInput">Current type.</typeparam>
    /// <param name="value">The actual instance.</param>
    /// <param name="actions">Actions to execute.</param>
    /// <returns>TInput If TInput is not null; otherwise default(TInput).</returns>
    public static TInput With<TInput>(this TInput value, params Action<TInput>[] actions) where TInput : class
    {
        if (value is null)
            return null;

        actions.ForEachInEnumerable(a => a(value));

        return value;
    }

    /// <summary>
    ///     Gets the value of objects property by its name.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj">The object.</param>
    /// <param name="name">The name.</param>
    /// <param name="index">
    ///     Optional index values for indexed properties.
    ///     The indexes of indexed properties are zero-based.
    ///     This value should be null for non-indexed properties.
    /// </param>
    /// <returns>Property</returns>
    public static T GetPropertyValueByName<T>(this object obj, string name, object[] index = null) =>
        (T)obj.GetType().GetProperty(name)?.GetValue(obj, index);

    public static T GetObject<T>(this object value) =>
        value is not null
            ? (T)value
            : default;

    public static string GetTypeInstanceDescription(this object value) =>
        value.GetType().GetTypeCustomAttribute<DescriptionAttribute>()?.FindInEnumerable()?.Description;

    public static bool IsBetween<T>(this T item, T start, T end, bool inclusive = false) =>
        Comparer<T>.Default.Compare(start, end) > 0 ? throw new ArgumentException("Given parameters create no range") :
        inclusive ? Comparer<T>.Default.Compare(item, start) >= 0 && Comparer<T>.Default.Compare(item, end) <= 0 :
        Comparer<T>.Default.Compare(item, start) > 0 && Comparer<T>.Default.Compare(item, end) < 0;

    /// <summary>
    ///     Determines whether the specified instance is in given instances set.
    /// </summary>
    /// <typeparam name="T">Type parameter</typeparam>
    /// <param name="item">The item.</param>
    /// <param name="items">The items to be checked.</param>
    /// <returns>
    ///     true if the specified instance is in given set; otherwise, false.
    /// </returns>
    public static bool IsIn<T>(this T item, params T[] items) => item.IsIn((IEnumerable<T>)items);

    public static bool IsIn<T>(this T item, IEnumerable<T> items) =>
        items switch
        {
            ISet<T> iSet => iSet.Contains(item),
            ICollection<T> col => col.Contains(item),
            _ => items.Contains(item)
        };

    public static bool IsNotIn<T>(this T item, params T[] items) => item.IsNotIn((IEnumerable<T>)items);

    public static bool IsNotIn<T>(this T item, IEnumerable<T> items) => !item.IsIn(items);

    /// <summary>
    ///     Wraps this object instance into an IEnumerable{T}
    ///     consisting of a single item.
    /// </summary>
    /// <typeparam name="T"> Type of the object. </typeparam>
    /// <param name="item"> The instance that will be wrapped. </param>
    /// <returns> An IEnumerable{T} consisting of a single item. </returns>
    public static IEnumerable<T> Yield<T>(this T item)
    {
        yield return item;
    }

    public static bool IsEqual<T>(ref T field, T value) => EqualityHelper.IsEqual(ref field, value);

    public static bool IsNotEqual<T>(ref T field, T value) => EqualityHelper.IsNotEqual(ref field, value);

    public static bool SetObjectProperty<TSender, T>(this TSender sender,
                                                     ref T backingField,
                                                     T newValue,
                                                     Action<TSender, string, T> onPropertyChanged,
                                                     [CallerMemberName] string propertyName = null)
    {
        if (!EqualityHelper.SetFieldIfChanged(ref backingField, newValue))
            return false;

        onPropertyChanged?.Invoke(sender, propertyName, newValue);

        return true;
    }

    public static bool SetProperty<TSender, TRet>(this TSender _,
                                                  ref TRet backingField,
                                                  TRet newValue,
                                                  Action<string, TRet> onPropertyChanged = null,
                                                  [CallerMemberName] string propertyName = null)
        where TSender : INotifyPropertyChanged
    {
        if (propertyName is null
            || EqualityComparer<TRet>.Default.Equals(backingField, newValue))
            return false;

        backingField = newValue;
        onPropertyChanged?.Invoke(propertyName, newValue);

        return true;
    }
}