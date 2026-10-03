using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
#if NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions for inspecting types, their attributes and their generic ancestry.</summary>
public static class TypeExtensions
{
    /// <summary>
    /// Gets the implemented interfaces.
    /// </summary>
    /// <param name="interfaceType">Type of the interface.</param>
    /// <returns></returns>
    public static IEnumerable<Type> GetImplementedInterfaces(this Type interfaceType) =>
        GetAllNotSealedClasses().Where(type => type.GetInterface(interfaceType.Name) is not null);

    /// <summary>
    /// Gets the implemented classes.
    /// </summary>
    /// <param name="baseType">Type of the base.</param>
    /// <returns></returns>
    public static IEnumerable<Type> GetImplementedClasses(this Type baseType) =>
        GetAllNotSealedClasses().Where(type => type.GetBaseTypes().Contains(baseType));

    /// <summary>
    /// Gets the base types.
    /// </summary>
    /// <param name="baseType">Type of the base.</param>
    /// <param name="baseTypes">The base types.</param>
    /// <returns></returns>
    public static List<Type> GetBaseTypes(this Type baseType, List<Type>? baseTypes = null)
    {
        baseTypes ??= [];

        if (baseType.BaseType is not null)
        {
            baseTypes.Add(baseType.BaseType);
            baseTypes = baseType.BaseType.GetBaseTypes(baseTypes);
        }

        return baseTypes;
    }

    /// <summary>Gets the description from the first <see cref="System.ComponentModel.DescriptionAttribute" /> declared on a type.</summary>
    /// <param name="value">The type to inspect.</param>
    /// <returns>The description, or null when the attribute is missing.</returns>
    public static string? GetTypeDescription(this Type value) =>
        value.GetTypeCustomAttribute<DescriptionAttribute>()?.FindInEnumerable()?.Description;

    /// <summary>Gets the custom attributes of a given type declared directly on a type.</summary>
    /// <typeparam name="TAttributeType">The attribute type.</typeparam>
    /// <param name="value">The type to inspect.</param>
    /// <returns>The matching attributes, without inherited ones.</returns>
    public static TAttributeType[] GetTypeCustomAttribute<TAttributeType>(this Type value)
        where TAttributeType : Attribute =>
        (TAttributeType[])value.GetCustomAttributes(typeof(TAttributeType), false);

    /// <summary>Determines whether a type is, derives from or implements a closed or open generic type.</summary>
    /// <param name="t">The type to test.</param>
    /// <param name="genericDefinition">The generic type to match; its generic type definition is compared.</param>
    /// <returns><c>true</c> if the type matches.</returns>
    public static bool IsGenericTypeOf(
#if NET9_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        this Type t,
        Type genericDefinition) =>
        t.IsGenericTypeOf(genericDefinition, out _);

    /// <summary>Determines whether a type is, derives from or implements a closed or open generic type, and returns the matched type arguments.</summary>
    /// <param name="t">The type to test.</param>
    /// <param name="genericDefinition">The generic type to match; its generic type definition is compared.</param>
    /// <param name="genericParameters">Receives the generic type arguments of the match, or an empty array when there is none.</param>
    /// <returns><c>true</c> if the type matches.</returns>
    public static bool IsGenericTypeOf(
#if NET9_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        this Type t,
        Type genericDefinition,
        out Type[] genericParameters)
    {
        genericParameters = [];

        if (!genericDefinition.GetTypeInfo().IsGenericType)
            return false;

        var isMatch = t.GetTypeInfo().IsGenericType
                      && t.GetGenericTypeDefinition() == genericDefinition.GetGenericTypeDefinition();

        var baseType = t.GetTypeInfo().BaseType;

        if (!isMatch
            && baseType is not null)
            isMatch = baseType.IsGenericTypeOf(genericDefinition, out genericParameters);

        if (!isMatch
            && genericDefinition.GetTypeInfo().IsInterface
            && t.GetTypeInfo().ImplementedInterfaces.Any())
            foreach (var i in t.GetTypeInfo().ImplementedInterfaces)
            {
#pragma warning disable IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
                if (i.IsGenericTypeOf(genericDefinition, out genericParameters))
                {
                    isMatch = true;

                    break;
                }
#pragma warning restore IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
            }

        if (isMatch && !genericParameters.Any())
            genericParameters = t.GenericTypeArguments;

        return isMatch;
    }

    /// <summary>Determines whether a type is the same as, or derives from or implements, another type.</summary>
    /// <param name="type">The type to test.</param>
    /// <param name="typeToCompare">The type to compare to.</param>
    /// <returns><c>true</c> if <paramref name="type" /> is or inherits from <paramref name="typeToCompare" />.</returns>
    public static bool IsOrInherits(this Type type, Type typeToCompare) =>
        type == typeToCompare || typeToCompare.IsAssignableFrom(type);

    /// <summary>
    /// Gets all not sealed classes.
    /// </summary>
    /// <returns></returns>
    private static IEnumerable<Type> GetAllNotSealedClasses() =>
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => t.IsClass && !t.IsSealed);
}