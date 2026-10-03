using System.ComponentModel;

namespace FEx.Agnostics.Abstractions.Enums;

/// <summary>Media types, whose descriptions hold the MIME type text.</summary>
public enum MediaTypes
{
    /// <summary>JSON, <c>application/json</c>.</summary>
    [Description("application/json")]
    ApplicationJson,

    /// <summary>SVG images, <c>image/svg+xml</c>.</summary>
    [Description("image/svg+xml")]
    ImageSvgXml,

    /// <summary>HTML, <c>text/html</c>.</summary>
    [Description("text/html")]
    TextHtml
}