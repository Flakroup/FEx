using FEx.Agnostics.Abstractions.Extensions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;

namespace FEx.Agnostics.Utilities;

public class FlakDynamicObject : DynamicObject, INotifyPropertyChanged
{
    /// <summary>
    /// The data
    /// </summary>
    private readonly ExpandoObject _data;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the <see cref="System.Object" /> with the specified column name.
    /// </summary>
    /// <value>
    /// The <see cref="System.Object" />.
    /// </value>
    /// <param name="columnName">Name of the column.</param>
    /// <returns></returns>
    public object? this[string columnName]
    {
        get =>
            Dic.ContainsKey(columnName)
                ? Dic[columnName]
                : null;
        set
        {
            if (!Dic.ContainsKey(columnName))
            {
                Dic.Add(columnName, value);

                OnPropertyChanged(columnName);
            }
            else
            {
                if (Dic[columnName] != value)
                {
                    Dic[columnName] = value;

                    OnPropertyChanged(columnName);
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="System.Object" /> with the specified column index.
    /// </summary>
    /// <value>
    /// The <see cref="System.Object" />.
    /// </value>
    /// <param name="columnIndex">Index of the column.</param>
    /// <returns></returns>
    public object? this[int columnIndex]
    {
        get =>
            columnIndex < Dic.Keys.Count
                ? Dic[Dic.Keys.ElementAt(columnIndex)]
                : null;
        set
        {
            while (columnIndex >= Dic.Keys.Count)
                Dic.Add(string.Empty, null);

            if (Dic[Dic.Keys.ElementAt(columnIndex)] != value)
            {
                Dic[Dic.Keys.ElementAt(columnIndex)] = value;

                OnPropertyChanged(Dic.Keys.ElementAt(columnIndex));
            }
        }
    }

    private IDictionary<string, object?> Dic => _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlakDynamicObject" /> class.
    /// </summary>
    public FlakDynamicObject()
    {
        _data = new();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlakDynamicObject" /> class.
    /// </summary>
    /// <param name="source">The source.</param>
    public FlakDynamicObject(IDictionary<string, object?> source)
    {
        _data = (ExpandoObject)source;
    }

    /// <summary>
    /// Returns the enumeration of all dynamic member names.
    /// </summary>
    /// <returns>
    /// A sequence that contains dynamic member names.
    /// </returns>
    public override IEnumerable<string> GetDynamicMemberNames() => Dic.Keys;

    /// <summary>
    /// Provides the implementation for operations that get member values. Classes derived from the
    /// <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for
    /// operations such as getting a value for a property.
    /// </summary>
    /// <param name="binder">
    /// Provides information about the object that called the dynamic operation. The binder.Name property
    /// provides the name of the member on which the dynamic operation is performed. For example, for the
    /// Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived
    /// from the <see cref="T:System.Dynamic.DynamicObject" /> class, binder.Name returns "SampleProperty". The
    /// binder.IgnoreCase property specifies whether the member name is case-sensitive.
    /// </param>
    /// <param name="result">
    /// The result of the get operation. For example, if the method is called for a property, you can
    /// assign the property value to <paramref name="result" />.
    /// </param>
    /// <returns>
    /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the
    /// language determines the behavior. (In most cases, a run-time exception is thrown.)
    /// </returns>
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        result = this[binder.Name];

        return true;
    }

    /// <summary>
    /// Provides the implementation for operations that set member values. Classes derived from the
    /// <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for
    /// operations such as setting a value for a property.
    /// </summary>
    /// <param name="binder">
    /// Provides information about the object that called the dynamic operation. The binder.Name property
    /// provides the name of the member to which the value is being assigned. For example, for the statement
    /// sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the
    /// <see cref="T:System.Dynamic.DynamicObject" /> class, binder.Name returns "SampleProperty". The binder.IgnoreCase
    /// property specifies whether the member name is case-sensitive.
    /// </param>
    /// <param name="value">
    /// The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where
    /// sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject" /> class, the
    /// <paramref name="value" /> is "Test".
    /// </param>
    /// <returns>
    /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the
    /// language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
    /// </returns>
    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        this[binder.Name] = value;

        return true;
    }

    /// <summary>
    /// Called when property has changed.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    private void OnPropertyChanged(string propertyName)
    {
        if (propertyName is null
            || PropertyChanged is null)
            return;

        PropertyChanged.HandlePropertyChanged(this, propertyName);
    }
}