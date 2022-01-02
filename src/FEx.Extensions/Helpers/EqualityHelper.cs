namespace FEx.Extensions.Helpers;

public static class EqualityHelper
{
    public static bool IsEqual<T>(ref T field, T value)
    {
        return EqualityComparer<T>.Default.Equals(field, value);
    }

    public static bool IsNotEqual<T>(ref T field, T value)
    {
        return !EqualityComparer<T>.Default.Equals(field, value);
    }

    public static bool IsEqual<T>(T field, T value)
    {
        return EqualityComparer<T>.Default.Equals(field, value);
    }

    public static bool IsNotEqual<T>(T field, T value)
    {
        return !EqualityComparer<T>.Default.Equals(field, value);
    }
}