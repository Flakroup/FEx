using System.ComponentModel;

namespace FEx.Agnostics.Abstractions.Enums;

public enum MediaTypes
{
    [Description("application/json")]
    ApplicationJson,

    [Description("image/svg+xml")]
    ImageSvgXml,

    [Description("text/html")]
    TextHtml
}