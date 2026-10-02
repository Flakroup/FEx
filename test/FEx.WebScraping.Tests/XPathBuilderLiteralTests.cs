using HtmlAgilityPack;
using Shouldly;
using Xunit;

namespace FEx.WebScraping.Tests;

/// <summary>
/// Values spliced into an XPath literal come from scraped content the caller does not control. A quote in
/// one must still yield a valid expression that matches the node, not an XPathException.
/// </summary>
public sealed class XPathBuilderLiteralTests
{
    [Theory]
    [InlineData("o'brien")]
    [InlineData("say \"hi\"")]
    [InlineData("o'brien says \"hi\"")]
    [InlineData("'\"")]
    [InlineData("plain")]
    public void EveryValueFilter_MatchesAValueContainingQuotes(string value)
    {
        HtmlDocument doc = new();
        HtmlNode div = doc.CreateElement("div");
        div.SetAttributeValue("class", value);
        div.SetAttributeValue("id", value);
        div.SetAttributeValue("data-x", value);
        div.AppendChild(doc.CreateTextNode(value + " tail"));
        doc.DocumentNode.AppendChild(div);

        XPathBuilderEx[] queries =
        [
            Div().WhereClass(value),
            Div().WhereId(value),
            Div().WhereAttributeEquals("data-x", value),
            Div().WhereStartWith("data-x", value),
            Div().WhereInnerTextContains(value)
        ];

        foreach (var query in queries)
            doc.DocumentNode.SelectSingleNode(query.ToString()).ShouldBe(div, query.ToString());

        doc.DocumentNode.SelectSingleNode(Div().WhereNotInnerTextContains(value).ToString()).ShouldBeNull();
    }

    [Fact]
    public void PlainValues_ProduceTheSameExpressionAsBefore()
    {
        Div().WhereClass("a b").ToString().ShouldBe("//div[@class='a b']");
        Div().WhereStartWith("id", "x").ToString().ShouldBe("//div[starts-with(@id,'x')]");
        Div().WhereInnerTextContains("t").ToString().ShouldBe("//div[contains(text(), 't')]");
    }

    [Theory]
    [InlineData("plain", "'plain'")]
    [InlineData("o'brien", "\"o'brien\"")]
    [InlineData("say \"hi\"", "'say \"hi\"'")]
    [InlineData("a'b\"c", "concat('a', \"'\", 'b\"c')")]
    public void Literal_PicksTheQuoteTheValueDoesNotContain(string value, string expected) =>
        XPathBuilderEx.Literal(value).ShouldBe(expected);

    private static XPathBuilderEx Div() => new XPathBuilderEx().ElementsDescend("div");
}
