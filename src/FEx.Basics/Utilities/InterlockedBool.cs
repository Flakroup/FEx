using System.Threading;

namespace FEx.Basics.Utilities;

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

    public InterlockedBool(bool value = false)
    {
        Value = value;
    }

    public static explicit operator bool(InterlockedBool obj) => obj.Value;

    public static explicit operator InterlockedBool(bool obj) => new(obj);

    public static bool operator ==(InterlockedBool obj1, bool obj2) => obj1.Value.Equals(obj2);

    public static bool operator !=(InterlockedBool obj1, bool obj2) => !obj1.Value.Equals(obj2);

    public static bool operator ==(bool obj1, InterlockedBool obj2) => obj1.Equals(obj2.Value);

    public static bool operator !=(bool obj1, InterlockedBool obj2) => !obj1.Equals(obj2.Value);

    public override bool Equals(object obj) =>
        ReferenceEquals(this, obj) || obj is InterlockedBool other && Equals(other);

    public override int GetHashCode() => _value;

    private bool Equals(InterlockedBool other) => _value == other._value;
}