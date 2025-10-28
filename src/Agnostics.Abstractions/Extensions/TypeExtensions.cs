using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
#if NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace FEx.Agnostics.Abstractions.Extensions;

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
    public static List<Type> GetBaseTypes(this Type baseType, List<Type> baseTypes = null)
    {
        baseTypes ??= [];

        if (baseType.BaseType is not null)
        {
            baseTypes.Add(baseType.BaseType);
            baseTypes = baseType.BaseType.GetBaseTypes(baseTypes);
        }

        return baseTypes;
    }

    public static string GetTypeDescription(this Type value) =>
        GetTypeCustomAttribute<DescriptionAttribute>(value)?.FindInEnumerable()?.Description;

    public static TAttributeType[] GetTypeCustomAttribute<TAttributeType>(this Type value)
        where TAttributeType : Attribute =>
        (TAttributeType[])value.GetCustomAttributes(typeof(TAttributeType), false);

    public static bool IsGenericTypeOf(
#if NET9_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        this Type t,
        Type genericDefinition) =>
        t.IsGenericTypeOf(genericDefinition, out _);

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

        if (!isMatch
            && t.GetTypeInfo().BaseType is not null)
            isMatch = t.GetTypeInfo().BaseType.IsGenericTypeOf(genericDefinition, out genericParameters);

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