using System.Threading;

namespace FEx.Agnostics.Abstractions.Utilities;

/// <summary>A boolean whose reads and writes are atomic and thread-safe.</summary>
public sealed class InterlockedBool
{
    private int _value;

    /// <summary>Gets or sets the value atomically.</summary>
    public bool Value
    {
        get => Interlocked.CompareExchange(ref _value, 1, 1) == 1;
        set
        {
            if (value)
                Interlocked.CompareExchange(ref _value, 1, 0);
            else
                Interlocked.CompareExchange(ref _value, 0, 1);
        }
    }

    /// <summary>Initializes the value to false.</summary>
    public InterlockedBool()
        : this(false)
    {
    }

    /// <summary>Initializes the value.</summary>
    /// <param name="value">The initial value.</param>
    public InterlockedBool(bool value)
    {
        Value = value;
    }

    /// <summary>Converts to the current boolean value.</summary>
    /// <param name="obj">The instance to convert.</param>
    public static explicit operator bool(InterlockedBool obj) => obj.Value;

    /// <summary>Creates an instance holding a boolean value.</summary>
    /// <param name="obj">The initial value.</param>
    public static explicit operator InterlockedBool(bool obj) => new(obj);

    /// <summary>Determines whether the current value equals a boolean.</summary>
    /// <param name="obj1">The interlocked boolean.</param>
    /// <param name="obj2">The boolean to compare with.</param>
    public static bool operator ==(InterlockedBool obj1, bool obj2) => obj1.Value.Equals(obj2);

    /// <summary>Determines whether the current value differs from a boolean.</summary>
    /// <param name="obj1">The interlocked boolean.</param>
    /// <param name="obj2">The boolean to compare with.</param>
    public static bool operator !=(InterlockedBool obj1, bool obj2) => !obj1.Value.Equals(obj2);

    /// <summary>Determines whether a boolean equals the current value.</summary>
    /// <param name="obj1">The boolean to compare.</param>
    /// <param name="obj2">The interlocked boolean.</param>
    public static bool operator ==(bool obj1, InterlockedBool obj2) => obj1.Equals(obj2.Value);

    /// <summary>Determines whether a boolean differs from the current value.</summary>
    /// <param name="obj1">The boolean to compare.</param>
    /// <param name="obj2">The interlocked boolean.</param>
    public static bool operator !=(bool obj1, InterlockedBool obj2) => !obj1.Equals(obj2.Value);

    /// <summary>Determines whether another object is the same instance or an <see cref="InterlockedBool" /> equal to this one.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><c>true</c> if the objects are equal.</returns>
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || obj is InterlockedBool other && Equals(other);

    /// <summary>Gets a hash code based on the current value.</summary>
    /// <returns>1 when the value is true; otherwise 0.</returns>
    public override int GetHashCode() => _value;

    /// <summary>
    /// This method sets a value
    /// </summary>
    /// <param name="value">Value to set</param>
    /// <returns>True if the value has been set or false if the value has already been set to the passed value</returns>
    public bool TrySet(bool value)
    {
        if (value)
            return Interlocked.CompareExchange(ref _value, 1, 0) == 0;

        return Interlocked.CompareExchange(ref _value, 0, 1) == 1;
    }

    private bool Equals(InterlockedBool other) => _value == other._value;
}