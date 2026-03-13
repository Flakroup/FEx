using HtmlAgilityPack;
using System;

namespace FEx.WebScraping.Extensions;

public static class HtmlNodeExtensions
{
    public const string Id = "link";
    public const string Link = "link";
    public const string Type = "type";
    public const string Href = "href";
    public const string Src = "src";
    public const string BodyPath = "/body[";

    /// <summary>
    /// Helper method to get the value of an attribute of this node. If the attribute is not found, the default value will
    /// be returned.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="name">The name of the attribute to get. May not be <c>null</c>.</param>
    /// <param name="def">The default value to return if not found.</param>
    /// <returns>The value of the attribute if found, the default value if not found.</returns>
    public static string GetNodeAttributeStringValue(this HtmlNode value, string name, string def = null) =>
        value.GetAttributeValue(name, def);

    public static bool GetNodeAttributeBoolValue(this HtmlNode value, string name, bool def = false) =>
        value.GetAttributeValue(name, def);

    public static int GetNodeAttributeIntValue(this HtmlNode value, string name, int def = 0) =>
        value.GetAttributeValue(name, def);

    public static string GetSrc(this HtmlNode value) => value.GetNodeAttributeStringValue(Src);

    public static string GetHref(this HtmlNode value) => value.GetNodeAttributeStringValue(Href);

    public static string GetNodeTypeAttributeValue(this HtmlNode value) => value.GetNodeAttributeStringValue(Type);

    public static bool IsNodeLinkElementOfType(this HtmlNode value, string type) =>
        type is not null
        && value.NodeType == HtmlNodeType.Element
        && value.Name == Link
        && value.GetNodeTypeAttributeValue() == type;

    public static bool IsInBody(this HtmlNode value) => value.XPath.Contains(BodyPath);

    /// <summary>
    /// Selects the first XmlNode that matches the XPath expression.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="xpath">The XPath expression. May not be null.</param>
    /// <returns>
    /// The first <see cref="HtmlNode" /> that matches the XPath query or a null reference if no matching
    /// node was found.
    /// </returns>
    public static HtmlNode SelectSingleNode(this HtmlNode value, Func<XPathBuilderEx, XPathBuilderEx> xpath) =>
        value.SelectSingleNode(xpath(new()));

    /// <summary>
    /// Selects a list of nodes matching the <see cref="P:HtmlAgilityPack.HtmlNode.XPath" /> expression.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="xpath">The XPath expression.</param>
    /// <returns>
    /// An <see cref="HtmlNodeCollection" /> containing a collection of nodes matching the
    /// <see cref="P:HtmlAgilityPack.HtmlNode.XPath" /> query, or <c>null</c> if no node matched the XPath expression.
    /// </returns>
    public static HtmlNodeCollection SelectNodes(this HtmlNode value, Func<XPathBuilderEx, XPathBuilderEx> xpath) =>
        value.SelectNodes(xpath(new()));
}