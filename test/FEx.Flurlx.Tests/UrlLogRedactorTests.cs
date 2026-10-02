using FEx.Flurlx.Services;
using Shouldly;
using System;
using System.Net.Http;
using Xunit;

namespace FEx.Flurlx.Tests;

public sealed class UrlLogRedactorTests
{
    [Theory]
    [InlineData("https://api.example.com/v1/x?api_key=SECRET123&user=bob", "https://api.example.com/v1/x?api_key=***&user=***")]
    [InlineData("https://alice:SECRET123@api.example.com:8443/v1/x", "https://***@api.example.com:8443/v1/x")]
    // Last '@' splits userinfo from host, so a password containing a raw '@' is fully masked.
    [InlineData("https://u:pa@SECRET123@h/p", "https://***@h/p")]
    // OData-style query with raw quotes and spaces: every parameter after the quote is still masked.
    [InlineData("https://api.example.com/odata/People?$filter=Name eq 'bob'&api_key=SECRET123", "https://api.example.com/odata/People?$filter=***&api_key=***")]
    [InlineData("https://api.example.com/odata/People?$filter=Name%20eq%20'bob'&api_key=SECRET123", "https://api.example.com/odata/People?$filter=***&api_key=***")]
    // Valueless query token may itself be the secret.
    [InlineData("https://api.example.com/x?SECRET123", "https://api.example.com/x?***")]
    [InlineData("https://api.example.com/x?SECRET123&page=2", "https://api.example.com/x?***&page=***")]
    [InlineData("https://h/x?a=1&&b=2", "https://h/x?a=***&&b=***")]
    [InlineData("https://h/x?", "https://h/x?")]
    // '?' after '#' belongs to the fragment, which is masked as a whole.
    [InlineData("https://h/p#a?token=SECRET123", "https://h/p#***")]
    [InlineData("https://h/p?token=SECRET123#frag", "https://h/p?token=***#***")]
    // Path segments are kept as documented (not covered by redaction).
    [InlineData("https://api.telegram.org/botTOKEN/sendMessage", "https://api.telegram.org/botTOKEN/sendMessage")]
    [InlineData("https://h", "https://h")]
    public void RedactUrl_MasksCredentialBearingParts(string url, string expected)
    {
        UrlLogRedactor.RedactUrl(url).ShouldBe(expected);
    }

    [Fact]
    public void RedactUrl_IsIdempotent()
    {
        var once = UrlLogRedactor.RedactUrl("https://u:p@h/x?k=SECRET123&t#f");

        UrlLogRedactor.RedactUrl(once).ShouldBe(once);
    }

    [Fact]
    public void RedactUrls_RedactsEveryUrlAndKeepsSurroundingText()
    {
        var text = UrlLogRedactor.RedactUrls(
            "Redirect from http://a.example/x?k=SECRET123\nto https://u:PW123@b.example/y failed");

        text.ShouldBe("Redirect from http://a.example/x?k=***\nto https://***@b.example/y failed");
    }

    [Fact]
    public void RedactUrls_MasksQueryUpToEndOfLine()
    {
        // A raw space in the query must not end the URL early and leave the following parameters unmasked.
        UrlLogRedactor.RedactUrls("GET https://h/x?q=a b&api_key=SECRET123")
            .ShouldBe("GET https://h/x?q=***&api_key=***");
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("Call timed out", "Call timed out")]
    [InlineData("see host:443 or C:\\temp", "see host:443 or C:\\temp")]
    public void RedactUrls_LeavesTextWithoutUrlsUnchanged(string? text, string expected)
    {
        UrlLogRedactor.RedactUrls(text).ShouldBe(expected);
    }

    [Fact]
    public void DescribeException_RedactsMessageAndHandlesNull()
    {
        UrlLogRedactor.DescribeException(null).ShouldBe("Unknown error");

        UrlLogRedactor.DescribeException(new HttpRequestException("GET https://h/x?api_key=SECRET123"))
            .ShouldBe("GET https://h/x?api_key=***");
    }

    [Fact]
    public void DescribeException_DoesNotIncludeInnerExceptionText()
    {
        var exception = new InvalidOperationException("outer", new HttpRequestException("https://h/?k=SECRET123"));

        UrlLogRedactor.DescribeException(exception).ShouldBe("outer");
    }
}
