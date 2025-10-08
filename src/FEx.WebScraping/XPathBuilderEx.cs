using HtmlAgilityPack;

namespace FEx.WebScraping;

public sealed class XPathBuilderEx : XPathBuilderBase<XPathBuilderEx>
{
    public XPathBuilderEx()
    {
    }

    public XPathBuilderEx(HtmlNode node = null)
        : base(node)
    {
    }

    public static implicit operator string(XPathBuilderEx builder) => builder.ToString();
}