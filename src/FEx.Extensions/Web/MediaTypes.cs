using System.ComponentModel;

namespace FEx.Extensions.Web;

public enum MediaTypes
{
    [Description("application/json")] ApplicationJson,

    [Description("image/svg+xml")] ImageSvgXml,

    [Description("text/html")] TextHtml
}