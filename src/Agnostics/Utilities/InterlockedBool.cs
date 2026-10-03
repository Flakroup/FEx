using System.Threading;

namespace FEx.Agnostics.Utilities;

/// <summary>A thread-safe boolean flag backed by interlocked operations.</summary>
public sealed class InterlockedBool
{
    private int _value;

    /// <summary>Gets or sets the flag value atomically.</summary>
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

    /// <summary>Initializes the flag.</summary>
    /// <param name="value">The initial value.</param>
    public InterlockedBool(bool value = false)
    {
        Value = value;
    }

    /// <summary>Converts the flag to its current boolean value.</summary>
    /// <param name="obj">The flag to read.</param>
    public static explicit operator bool(InterlockedBool obj) => obj.Value;

    /// <summary>Creates a flag with the given initial value.</summary>
    /// <param name="obj">The initial value.</param>
    public static explicit operator InterlockedBool(bool obj) => new(obj);

    /// <summary>Determines whether the flag currently equals the boolean value.</summary>
    /// <param name="obj1">The flag.</param>
    /// <param name="obj2">The boolean to compare with.</param>
    /// <returns><see langword="true"/> if they are equal.</returns>
    public static bool operator ==(InterlockedBool obj1, bool obj2) => obj1.Value.Equals(obj2);

    /// <summary>Determines whether the flag currently differs from the boolean value.</summary>
    /// <param name="obj1">The flag.</param>
    /// <param name="obj2">The boolean to compare with.</param>
    /// <returns><see langword="true"/> if they differ.</returns>
    public static bool operator !=(InterlockedBool obj1, bool obj2) => !obj1.Value.Equals(obj2);

    /// <summary>Determines whether two flags are the same instance or hold the same value.</summary>
    /// <param name="obj1">The first flag.</param>
    /// <param name="obj2">The second flag.</param>
    /// <returns><see langword="true"/> if they are equal.</returns>
    public static bool operator ==(InterlockedBool? obj1, InterlockedBool? obj2) =>
        ReferenceEquals(obj1, obj2) || obj1 is not null && obj2 is not null && obj1.Equals(obj2);

    /// <summary>Determines whether two flags differ in identity and value.</summary>
    /// <param name="obj1">The first flag.</param>
    /// <param name="obj2">The second flag.</param>
    /// <returns><see langword="true"/> if they differ.</returns>
    public static bool operator !=(InterlockedBool? obj1, InterlockedBool? obj2) => !(obj1 == obj2);

    /// <summary>Determines whether the boolean value equals the flag's current value.</summary>
    /// <param name="obj1">The boolean.</param>
    /// <param name="obj2">The flag to compare with.</param>
    /// <returns><see langword="true"/> if they are equal.</returns>
    public static bool operator ==(bool obj1, InterlockedBool obj2) => obj1.Equals(obj2.Value);

    /// <summary>Determines whether the boolean value differs from the flag's current value.</summary>
    /// <param name="obj1">The boolean.</param>
    /// <param name="obj2">The flag to compare with.</param>
    /// <returns><see langword="true"/> if they differ.</returns>
    public static bool operator !=(bool obj1, InterlockedBool obj2) => !obj1.Equals(obj2.Value);

    /// <summary>Determines whether the object is the same flag or a flag holding the same value.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true"/> if they are equal.</returns>
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || obj is InterlockedBool other && Equals(other);

    /// <summary>Returns the current flag state as the hash code.</summary>
    /// <returns>1 when the flag is set; otherwise 0.</returns>
    public override int GetHashCode() => _value;

    private bool Equals(InterlockedBool other) => _value == other._value;
}