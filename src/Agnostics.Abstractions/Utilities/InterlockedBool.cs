using System.Threading;

namespace FEx.Agnostics.Abstractions.Utilities;

public sealed class InterlockedBool
{
    private int _value;

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

    public InterlockedBool()
        : this(false)
    {
    }

    public InterlockedBool(bool value)
    {
        Value = value;
    }

    public static explicit operator bool(InterlockedBool obj) => obj.Value;

    public static explicit operator InterlockedBool(bool obj) => new(obj);

    public static bool operator ==(InterlockedBool obj1, bool obj2) => obj1.Value.Equals(obj2);

    public static bool operator !=(InterlockedBool obj1, bool obj2) => !obj1.Value.Equals(obj2);

    public static bool operator ==(bool obj1, InterlockedBool obj2) => obj1.Equals(obj2.Value);

    public static bool operator !=(bool obj1, InterlockedBool obj2) => !obj1.Equals(obj2.Value);

    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || obj is InterlockedBool other && Equals(other);

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