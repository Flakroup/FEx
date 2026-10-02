using HtmlAgilityPack;
using System;
using System.Text;

namespace FEx.WebScraping;

public abstract class XPathBuilderBase<T> where T : XPathBuilderBase<T>, new()
{
    protected StringBuilder SB { get; }

    protected XPathBuilderBase()
        : this(null)
    {
    }

    protected XPathBuilderBase(HtmlNode? node)
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
        ConcatXPath($"[@{attributeName}={Literal(attributeValue)}]");

    public T WhereClass(string className) => ConcatXPath($"[@class={Literal(className)}]");

    public T WhereId(string id) => ConcatXPath($"[@id={Literal(id)}]");

    public T WhereIndex(int number) =>
        number < 1
            ? throw new("The number needs to be greater than 0")
            : ConcatXPath($"[{number}]");

    public T WhereInnerTextEquals(string value) => ConcatXPath($"[text()={Literal(value)}]");

    public T WhereInnerTextContains(string value) => ConcatXPath($"[contains(text(), {Literal(value)})]");

    public T WhereLast() => ConcatXPath("[last()]");

    public T WhereLastMinus(int number) =>
        number < 1
            ? throw new("The number need to be greater than 0")
            : ConcatXPath($"[last()-{number}]");

    public T WhereNotInnerTextContains(string value) => ConcatXPath($"[not(contains(text(), {Literal(value)}))]");

    public T WhereClassStartWith(string attributeValue) => WhereStartWith("class", attributeValue);

    public T WhereStartWith(string attributeName, string attributeValue) =>
        ConcatXPath($"[starts-with(@{attributeName},{Literal(attributeValue)})]");

    public T WhereStartWithId(string attributeValue) => WhereStartWith("id", attributeValue);

    public T WithExpression(Func<T, T> xPath) => WithExpression(xPath(new()));

    public T WithExpression(T xPath) => WithExpression(xPath.ToString());

    public T WithExpression(string xPath) => ConcatXPath($"({xPath})");

    /// <summary>
    /// Quotes <paramref name="value" /> as an XPath 1.0 string literal. XPath has no escape sequence inside a
    /// literal, so a value is wrapped in whichever quote it does not contain, and one containing both is
    /// built with <c>concat()</c>. A value without quotes comes out as <c>'value'</c>, exactly as before.
    /// </summary>
    public static string Literal(string value)
    {
        if (value.IndexOf('\'') < 0)
            return $"'{value}'";

        if (value.IndexOf('"') < 0)
            return $"\"{value}\"";

        // Both quote kinds: single-quote every run between apostrophes and splice the apostrophes back in
        // as "'" - e.g. a'b"c becomes concat('a', "'", 'b"c').
        return $"concat('{value.Replace("'", "', \"'\", '")}')";
    }

    protected T ConcatXPath(string xPath)
    {
        SB.Append(xPath);

        return (T)this;
    }
}