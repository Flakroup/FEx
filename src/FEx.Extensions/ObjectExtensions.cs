using FEx.Extensions.Base.Helpers;
using FEx.Extensions.Collections.Enumerables;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Guardian = GuardNet.Guard;

namespace FEx.Extensions;

/// <summary>
///     Object extensions class.
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    ///     Gets the specified field and initialized it if needed.
    /// </summary>
    /// <typeparam name="TField">Field type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="field">The field.</param>
    /// <param name="initializer">The initializer.</param>
    /// <returns>Field value.</returns>
    // ReSharper disable UnusedParameter.Global
    public static TField Get<TField>(this object value, ref TField field, Func<TField> initializer)
        // ReSharper restore UnusedParameter.Global
    {
        field ??= initializer();

        return field;
    }

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
        (value as IDisposable)?.Dispose();
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
    ///     Checks an value to ensure it isn't null.
    /// </summary>
    /// <typeparam name="T">Current type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">Name of the parameter.</param>
    /// <param name="message">The message to display.</param>
    /// <returns>
    ///     The value itself.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is a null reference.</exception>
    public static T Guard<T>(this T value, [CallerMemberName] string paramName = null, string message = null)
    {
        return value.Guard(v => v is null, paramName, message);
    }

    /// <summary>
    ///     Checks an value to ensure it comply to the condition we provide.
    /// </summary>
    /// <typeparam name="T">Current type.</typeparam>
    /// <param name="value">Current value.</param>
    /// <param name="func">The condition to test.</param>
    /// <param name="paramName">Name of the parameter.</param>
    /// <param name="message">The message to display.</param>
    /// <returns>
    ///     The value itself.
    /// </returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <remarks>
    ///     Throws a <see cref="ArgumentNullException" /> if the condition is false.
    /// </remarks>
    public static T Guard<T>(this T value, Func<T, bool> func, string paramName, string message = null)
    {
        Guardian.For(() =>
        {
            if (func(value))
                return true;
            return false;
        }, new ArgumentNullException(paramName, message));

        return value;
    }

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
            return default;

        actions.ForEachInEnumerable(a => a(value));
        return value;
    }

    /// <summary>
    ///     Gets a string representation of the objects property values.
    /// </summary>
    /// <param name="source">The object for the string representation.</param>
    /// <param name="name">The name of the object.</param>
    /// <returns>A string of properties.</returns>
    public static string ToPropertiesString(this object source, string name) =>
        source.ToPropertiesString(source.GetType(), name);

    /// <summary>
    ///     Gets a string representation of the objects property values, with a delimiter between values.
    /// </summary>
    /// <param name="obj">The object for the string representation.</param>
    /// <param name="type">The type of the object.</param>
    /// <param name="name">The name of the object.</param>
    /// <returns>A string of properties.</returns>
    public static string ToPropertiesString(this object obj, Type type, string name)
    {
        if (obj is not null)
        {
            var propertyString = new StringBuilder();
            string objNameSegment = name is not null
                ? name + " = "
                : string.Empty;

            if (Convert.GetTypeCode(obj) == TypeCode.Object
                && obj is not IEnumerable
                && type != typeof(Guid))
            {
                // if object, get all properties
                propertyString.Append("(")
                    .Append(type.FullName)
                    .Append(" ")
                    .Append(objNameSegment)
                    .Append(") Properties: ")
                    .Append(obj.ToPropertiesString());
            }
            else
            {
                if (type == typeof(Guid))
                    propertyString.Append("(")
                        .Append(type.Name)
                        .Append(" ")
                        .Append(name)
                        .Append(" = '")
                        .Append(type.GUID)
                        .Append("')");
                else
                    // for primitive types, just show the type and value
                    // for collection types, just show the collection type and item type (e.g. [(List`1)  'System.Collections.Generic.List`1[JCDCHelper.CV.DDLDispValueCV]'])
                    propertyString.Append("(")
                        .Append(type.Name)
                        .Append(" ")
                        .Append(objNameSegment)
                        .Append(" '")
                        .Append(obj)
                        .Append("')");
            }

            return propertyString.ToString();
        }

        return " None ";
    }

    /// <summary>
    ///     Gets a string representation of the objects property values, with a delimiter between values.
    /// </summary>
    /// <param name="obj">The object for the string representation.</param>
    /// <returns>A string of properties.</returns>
    public static string ToPropertiesString(this object obj)
    {
        var propertiesString = new StringBuilder();
        foreach (PropertyInfo property in obj.GetType().GetProperties())
        {
            string name = property.Name;
            object value = property.GetValue(obj, null);
            propertiesString.Append("(")
                .Append(property.PropertyType.Name)
                .Append(") ")
                .Append(name)
                .Append(" = '")
                .Append(value is null
                    ? "null"
                    : value)
                .Append("', ");
        }

        // remove last comma
        if (propertiesString.Length > 0)
            propertiesString.Remove(propertiesString.Length - 2, 2);

        return propertiesString.ToString();
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

    public static bool IsIn<T>(this T item, IEnumerable<T> items)
    {
        return items switch
        {
            ISet<T> iSet => iSet.Contains(item),
            ICollection<T> col => col.Contains(item),
            _ => items.Contains(item)
        };
    }

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
}