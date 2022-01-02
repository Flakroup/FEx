using FEx.Extensions.Collections.Enumerables;
using System.ComponentModel;

namespace FEx.Extensions;

public static class TypeExtensions
{
    /// <summary>
    ///     Gets the implemented interfaces.
    /// </summary>
    /// <param name="interfaceType">Type of the interface.</param>
    /// <returns></returns>
    public static IEnumerable<Type> GetImplementedInterfaces(this Type interfaceType)
    {
        return GetAllNotSealedClasses().Where(type => type.GetInterface(interfaceType.Name) != null);
    }

    /// <summary>
    ///     Gets the implemented classes.
    /// </summary>
    /// <param name="baseType">Type of the base.</param>
    /// <returns></returns>
    public static IEnumerable<Type> GetImplementedClasses(this Type baseType)
    {
        return GetAllNotSealedClasses().Where(type => type.GetBaseTypes().Contains(baseType));
    }

    /// <summary>
    ///     Gets the base types.
    /// </summary>
    /// <param name="baseType">Type of the base.</param>
    /// <param name="baseTypes">The base types.</param>
    /// <returns></returns>
    public static List<Type> GetBaseTypes(this Type baseType, List<Type> baseTypes = null)
    {
        if (baseTypes == null)
        {
            baseTypes = new List<Type>();
        }

        if (baseType.BaseType != null)
        {
            baseTypes.Add(baseType.BaseType);
            baseTypes = baseType.BaseType.GetBaseTypes(baseTypes);
        }

        return baseTypes;
    }

    public static string GetTypeDescription(this Type value)
    {
        return GetTypeCustomAttribute<DescriptionAttribute>(value)?.Find()?.Description;
    }

    public static TAttributeType[] GetTypeCustomAttribute<TAttributeType>(this Type value)
        where TAttributeType : Attribute
    {
        return (TAttributeType[])value.GetCustomAttributes(typeof(TAttributeType), false);
    }

    /// <summary>
    ///     Gets all not sealed classes.
    /// </summary>
    /// <returns></returns>
    private static IEnumerable<Type> GetAllNotSealedClasses()
    {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).Where(t => t.IsClass && !t.IsSealed);
    }
}