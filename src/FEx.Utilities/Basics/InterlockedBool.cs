using System.Threading;

namespace FEx.Utilities.Basics
{
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

        private bool Equals(InterlockedBool other)
        {
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is InterlockedBool other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public static explicit operator bool(InterlockedBool obj)
        {
            return obj.Value;
        }

        public static explicit operator InterlockedBool(bool obj)
        {
            return new(obj);
        }

        public static bool operator ==(InterlockedBool obj1, bool obj2)
        {
            return obj1.Value.Equals(obj2);
        }

        public static bool operator !=(InterlockedBool obj1, bool obj2)
        {
            return !obj1.Value.Equals(obj2);
        }

        public static bool operator ==(bool obj1, InterlockedBool obj2)
        {
            return obj1.Equals(obj2.Value);
        }

        public static bool operator !=(bool obj1, InterlockedBool obj2)
        {
            return !obj1.Equals(obj2.Value);
        }
    }
}