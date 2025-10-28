using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>
/// Extension methods for the Dynamic
/// </summary>
public static class DynamicExtensions
{
    /// <summary>
    /// Gets the dynamic member names.
    /// </summary>
    /// <param name="expandoObject">The expando object.</param>
    /// <returns></returns>
    public static IEnumerable<string> GetDynamicMemberNames(this ExpandoObject expandoObject) =>
        ((IDictionary<string, object>)expandoObject).Keys;

    /// <summary>
    /// Adds the properties from dictionary.
    /// </summary>
    /// <param name="eo">The eo.</param>
    /// <param name="propsDictionary">The props dictionary.</param>
    /// <returns>
    /// dynamic
    /// </returns>
    public static dynamic AddPropertiesFromDictionary(this ExpandoObject eo,
                                                      IDictionary<string, object> propsDictionary)
    {
        ICollection<KeyValuePair<string, object>> eoColl = eo;

        foreach (KeyValuePair<string, object> kvp in propsDictionary)
            eoColl.Add(kvp);

        return eo;
    }

    public static IDictionary<string, object> DynamicObjectToDictionary(dynamic src) =>
        src is not null
            ? ((PropertyInfo[])src.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)).ToDictionary(
                prop => prop.Name,
                prop => prop.GetValue(src, null))
            : new Dictionary<string, object>();
}