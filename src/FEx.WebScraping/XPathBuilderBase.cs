using HtmlAgilityPack;
using System;
using System.Text;

namespace FEx.WebScraping;

public abstract class XPathBuilderBase<T> where T : XPathBuilderBase<T>, new()
{
    protected StringBuilder SB { get; }

    protected XPathBuilderBase(HtmlNode node = null)
    {
        SB = new();

        if (node is not null)
            SB.Append(node.XPath);
    }

    public override string ToString() => SB.ToString();

    public T This() => ConcatXPath(".");

    public T AllChildElements() => ConcatXPath("/*");

    public T AllComments() => ConcatXPath("//comment()");

    public T AllDescedentElements() => ConcatXPath("//*");

    public T Attributes(string attributeName) => ConcatXPath($"/@{attributeName}");

    public T Elements(string element) => ConcatXPath($"/{element}");

    public T ElementsAtLeastAnyAttribute(string element) => ConcatXPath($"/{element}[@*]");

    public T ElementsDescend(string element) => ConcatXPath($"//{element}");

    public T First() => ConcatXPath("[1]");

    public T First(int number) =>
        number < 1
            ? throw new("The number need to be greaten 0")
            : ConcatXPath($"[position()<{number + 1}]");

    public T Index(int index) => ConcatXPath($"[{index}]");

    public T InnerText() => ConcatXPath("/text()");

    public T Or() => ConcatXPath("|");

    public T Parent() => ConcatXPath("/..");

    public T WhereAttributeEquals(string attributeName, string attributeValue) =>
        ConcatXPath($"[@{attributeName}='{attributeValue}']");

    public T WhereClass(string className) => ConcatXPath($"[@class='{className}']");

    public T WhereId(string id) => ConcatXPath($"[@id='{id}']");

    public T WhereIndex(int number) =>
        number < 1
            ? throw new("The number needs to be greater than 0")
            : ConcatXPath($"[{number}]");

    public T WhereInnerTextEquals(string value) => ConcatXPath($"[@text()='{value}']");

    public T WhereInnerTextContains(string value) => ConcatXPath($"[contains(text(), '{value}')]");

    public T WhereLast() => ConcatXPath("[last()]");

    public T WhereLastMinus(int number) =>
        number < 1
            ? throw new("The number need to be greater than 0")
            : ConcatXPath($"[last()-{number}]");

    public T WhereNotInnerTextContains(string value) => ConcatXPath($"[not(contains(text(), '{value}'))]");

    public T WhereClassStartWith(string attributeValue) => WhereStartWith("class", attributeValue);

    public T WhereStartWith(string attributeName, string attributeValue) =>
        ConcatXPath($"[starts-with(@{attributeName},'{attributeValue}')]");

    public T WhereStartWithId(string attributeValue) => WhereStartWith("id", attributeValue);

    public T WithExpression(Func<T, T> xPath) => WithExpression(xPath(new()));

    public T WithExpression(T xPath) => WithExpression(xPath.ToString());

    public T WithExpression(string xPath) => ConcatXPath($"({xPath})");

    protected T ConcatXPath(string xPath)
    {
        SB.Append(xPath);

        return (T)this;
    }
}