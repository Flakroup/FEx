using System;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class BooleanExtensions
{
    /// <summary>
    /// Combines 2 boolean (And Not operation).
    /// </summary>
    /// <param name="first">The first boolean.</param>
    /// <param name="second">The second boolean.</param>
    /// <returns>True if first boolean is True and second boolean is False; otherwise False.</returns>
    public static bool AndNot(this bool first, bool second) => first && !second;

    /// <summary>
    /// Combines 2 boolean (And operation).
    /// </summary>
    /// <param name="first">The first boolean.</param>
    /// <param name="second">The second boolean.</param>
    /// <returns>True if both boolean is valid; otherwise False.</returns>
    public static bool AndAlso(this bool first, bool second) => first && second;

    /// <summary>
    /// Combines 2 boolean (Or operation).
    /// </summary>
    /// <param name="first">The first boolean.</param>
    /// <param name="second">The second boolean.</param>
    /// <returns>True if if any of boolean is valid; otherwise False.</returns>
    public static bool OrElse(this bool first, bool second) => first || second;

    /// <summary>
    /// Gets a int from a boolean.
    /// </summary>
    /// <param name="value">The boolean itself.</param>
    /// <returns>1 if value is True otherwise 0.</returns>
    public static int ToInteger(this bool value) =>
        value
            ? 1
            : 0;

    /// <summary>
    /// Gets a value indicating if the value is False
    /// </summary>
    /// <param name="value">The boolean itself.</param>
    /// <returns>True if value is False otherwise True.</returns>
    public static bool IsFalse(this bool value) => !value;

    /// <summary>
    /// Execute func if value is True.
    /// </summary>
    /// <typeparam name="T">Type of return value.</typeparam>
    /// <param name="value">The boolean itself.</param>
    /// <param name="func">Function to execute.</param>
    /// <returns>T if value is True; otherwise default value of T.</returns>
    /// <remarks>
    /// Ex: var test = (true.IfTrue(() => false));
    /// </remarks>
    public static T IfTrue<T>(this bool value, Func<T> func) =>
        value
            ? func()
            : default;

    /// <summary>
    /// Execute action if value is True.
    /// </summary>
    /// <param name="value">The boolean itself.</param>
    /// <param name="action">Action to execute.</param>
    /// <remarks>
    /// Ex: true.IfTrue(() => MyMethod()));
    /// </remarks>
    public static void IfTrue(this bool value, Action action)
    {
        if (value)
            action();
    }

    /// <summary>
    /// Execute func if value is False.
    /// </summary>
    /// <typeparam name="T">Type of return value.</typeparam>
    /// <param name="value">The boolean itself.</param>
    /// <param name="func">Function to execute.</param>
    /// <returns>T if value is False; otherwise default value of T.</returns>
    /// <remarks>
    /// Ex: var test = (false.IfFalse(() => true));
    /// </remarks>
    public static T IfFalse<T>(this bool value, Func<T> func) =>
        !value
            ? func()
            : default;

    /// <summary>
    /// Execute action if value is False.
    /// </summary>
    /// <param name="value">The boolean itself.</param>
    /// <param name="action">Action to execute.</param>
    /// <remarks>
    /// Ex: false.IfFalse(() => MyMethod()));
    /// </remarks>
    public static void IfFalse(this bool value, Action action)
    {
        if (!value)
            action();
    }

    /// <summary>
    /// Execute action if value is True/False.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">The boolean itself.</param>
    /// <param name="funcTrue">Function to execute if expression is True.</param>
    /// <param name="funcFalse">Function to execute if expression is False.</param>
    /// <returns>
    /// T
    /// </returns>
    /// <remarks>
    /// Ex: true.IfTrueOrFalse(() =} MyMethodTrue(), () =} MyMethodFalse());
    /// </remarks>
    public static T IfTrueOrFalse<T>(this bool value, Func<T> funcTrue, Func<T> funcFalse) =>
        value
            ? funcTrue()
            : funcFalse();

    /// <summary>
    /// Execute action if value is True/False.
    /// </summary>
    /// <param name="value">The boolean itself.</param>
    /// <param name="actionTrue">Action to execute if expression is True.</param>
    /// <param name="actionFalse">Action to execute if expression is False.</param>
    /// <remarks>
    /// Ex: true.IfTrueOrFalse(() => MyMethodTrue(), () => MyMethodFalse());
    /// </remarks>
    public static void IfTrueOrFalse(this bool value, Action actionTrue, Action actionFalse)
    {
        if (value)
            actionTrue();
        else
            actionFalse();
    }

    /// <summary>
    /// Checks an condition for True/False.
    /// </summary>
    /// <param name="condition">Condition to test.</param>
    /// <param name="argumentName">The name of the argument.</param>
    /// <exception cref="ArgumentNullException"><paramref name="condition" /> Condition is a false.</exception>
    public static void ThrowArgumentOutOfRange(this bool condition, string argumentName)
    {
        if (!condition)
            throw new ArgumentOutOfRangeException(argumentName);
    }

    /// <summary>
    /// Converts the nullable boolean to a boolean.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as bool it not null; Otherwise false.</returns>
    public static bool ToBooleanSafe(this bool? value) => value ?? false;

    /// <summary>
    /// Converts the nullable boolean to a boolean.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>The value as bool it not null; Otherwise default value.</returns>
    public static bool ToBooleanSafe(this bool? value, bool defaultValue) => value ?? defaultValue;

    /// <summary>
    /// Converts the nullable boolean to a string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value as string it not null; Otherwise an false string.</returns>
    public static string ToStringSafe(this bool? value) => value?.ToString() ?? "false";
}