using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Numericals;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class XElementExtensions
{
    /// <summary>
    ///     Convert to a XML element.
    /// </summary>
    /// <param name="el">The xelement to convert.</param>
    /// <returns>A XmlElement.</returns>
    public static IXPathNavigable ToXmlElement(this XNode el)
    {
        var doc = new XmlDocument();
        doc.Load(el.CreateReader());

        return doc.DocumentElement;
    }

    /// <summary>
    ///     Gets a attribute value.
    /// </summary>
    /// <param name="element">Current element.</param>
    /// <param name="attributeName">Name of the attribute to get value for.</param>
    /// <returns>The value if found; otherwise a empty string.</returns>
    public static string GetAttributeValue(this XElement element, string attributeName)
    {
        attributeName.Guard(nameof(attributeName));

        XName name = attributeName;

        return element.Attribute(name) is not null
            ? element.Attribute(name)?.Value
            : string.Empty;
    }

    /// <summary>
    ///     Sets the value for an attribute.
    /// </summary>
    /// <typeparam name="T">Type of value.</typeparam>
    /// <param name="attribute">Current attribute.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The attribute itself.</returns>
    public static XAttribute SetValue<T>(this XAttribute attribute, T value)
    {
        attribute.SetValue(value);

        return attribute;
    }

    /// <summary>
    ///     Creates a attribute.
    /// </summary>
    /// <param name="attribute">Current attribute.</param>
    /// <param name="attributeName">Name for the attribute.</param>
    /// <returns>The created attribute.</returns>
    public static XAttribute Create(this XObject attribute, string attributeName) =>
        attribute.Create(attributeName, string.Empty);

    /// <summary>
    ///     Creates a attribute.
    /// </summary>
    /// <param name="attribute">Current attribute.</param>
    /// <param name="attributeName">Name for the attribute.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The created attribute.</returns>
    public static XAttribute Create(this XObject attribute, string attributeName, string value) =>
        attribute.Parent.GetOrCreateAttribute(attributeName).SetValue<string>(value);

    /// <summary>
    ///     Creates a attribute.
    /// </summary>
    /// <param name="element">Current element.</param>
    /// <param name="attributeName">Name for the attribute.</param>
    /// <returns>The element itself.</returns>
    public static XContainer CreateAttribute(this XContainer element, string attributeName)
    {
        element.CreateAttribute(attributeName, string.Empty);

        return element;
    }

    /// <summary>
    ///     Creates a attribute.
    /// </summary>
    /// <param name="element">Current element.</param>
    /// <param name="attributeName">Name for the attribute.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The element itself.</returns>
    public static XContainer CreateAttribute(this XContainer element, string attributeName, string value)
    {
        attributeName.Guard(nameof(attributeName));
        XName name = attributeName;
        element.Add(new XAttribute(name, value));

        return element;
    }

    /// <summary>
    ///     Gets a attribute (if the attribute not exist it's created).
    /// </summary>
    /// <param name="element">Current element.</param>
    /// <param name="attributeName">Name for the attribute.</param>
    /// <returns>The created attribute.</returns>
    public static XAttribute GetOrCreateAttribute(this XElement element, string attributeName)
    {
        attributeName.Guard(nameof(attributeName));

        XAttribute attribute = null;
        XName name = attributeName;

        (element.Attribute(name) is null).IfTrueOrFalse(() => attribute = new(name, string.Empty),
            () => attribute = element.Attribute(name));

        return attribute;
    }

    /// <summary>
    ///     Gets a value for a child.
    /// </summary>
    /// <param name="element">Current element.</param>
    /// <param name="childName">Name for the child.</param>
    /// <returns>The value if found; otherwise a empty string.</returns>
    /// <remarks>
    ///     If you have more than one child with the same name, the first child will be returned.
    /// </remarks>
    public static string GetChildValue(this XElement element, string childName) =>
        element.GetChildValue(childName, string.Empty);

    /// <summary>
    ///     Gets the child value.
    /// </summary>
    /// <param name="element">Current element.</param>
    /// <param name="childName">Name for the child.</param>
    /// <param name="nameSpace">The name space.</param>
    /// <returns>The value if found; otherwise a empty string.</returns>
    /// <remarks>
    ///     If you have more than one child with the same name, the first child will be returned.
    /// </remarks>
    public static string GetChildValue(this XElement element, string childName, string nameSpace)
    {
        childName.Guard(nameof(childName));

        var name = XName.Get(childName, nameSpace);

        return !element.HasElements
            ? string.Empty
            :
            element.Elements(name).Any() && element.Elements(name).FirstOrDefault() is not null
                ?
                element.Elements(name).First().Value
                : string.Empty;
    }

    /// <summary>
    ///     Gets the child.
    /// </summary>
    /// <param name="element">Current element.</param>
    /// <param name="childName">Name for the child.</param>
    /// <returns>The child if found; otherwise a null.</returns>
    public static XElement GetChild(this XElement element, string childName) =>
        element.GetChild(childName, string.Empty);

    /// <summary>
    ///     Gets the child.
    /// </summary>
    /// <param name="element">Current element.</param>
    /// <param name="childName">Name for the child.</param>
    /// <param name="nameSpace">The name space.</param>
    /// <returns>The child if found; otherwise a null.</returns>
    public static XElement GetChild(this XElement element, string childName, string nameSpace)
    {
        childName.Guard(nameof(childName));

        var name = XName.Get(childName, nameSpace);

        return !element.HasElements
            ? null
            :
            element.Elements(name).Any() && element.Elements(name).FirstOrDefault() is not null
                ?
                element.Elements(name).First()
                : null;
    }

    /// <summary>
    ///     Creates a child element.
    /// </summary>
    /// <param name="element">Current element.</param>
    /// <param name="childName">Name for the child.</param>
    /// <returns>The created child element.</returns>
    public static XElement CreateChild(this XContainer element, string childName) =>
        element.CreateChild(childName, string.Empty);

    /// <summary>
    ///     Creates a child element.
    /// </summary>
    /// <param name="element">Current element.</param>
    /// <param name="childName">Name for the child.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The created child element.</returns>
    public static XElement CreateChild(this XContainer element, string childName, string value)
    {
        childName.Guard(nameof(childName));

        XName name = childName;
        var child = new XElement(name, value);
        element.Add(child);

        return child;
    }

    /// <summary>
    ///     Sets the value for an element.
    /// </summary>
    /// <param name="element">Current element.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The element itself.</returns>
    public static XElement SetChildValue(this XElement element, string value)
    {
        element.Value = value;

        return element;
    }

    /// <summary>
    ///     Gets a element (if the element not exist it's created).
    /// </summary>
    /// <param name="element">Current element.</param>
    /// <param name="childName">Name for the element.</param>
    /// <returns>The created element or the first child with the name.</returns>
    public static XElement GetOrCreateChild(this XElement element, string childName) =>
        element.GetOrCreateChild(childName, string.Empty);

    /// <summary>
    ///     Gets a element (if the element not exist it's created).
    /// </summary>
    /// <param name="element">Current element.</param>
    /// <param name="childName">Name for the element.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The created element or the first child with the name.</returns>
    public static XElement GetOrCreateChild(this XElement element, string childName, string value)
    {
        childName.Guard(nameof(childName));

        XName name = childName;
        var child = new XElement(name, value);

        if (!element.HasElements
            || element.Elements(name).Count().IsZero())
        {
            element.Add(child);

            return child;
        }

        return element.Elements(name).First();
    }
}