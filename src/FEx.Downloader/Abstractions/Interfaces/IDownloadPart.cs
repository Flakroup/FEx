using System.Net.Http.Headers;

namespace FEx.Downloader.Abstractions.Interfaces;

public interface IDownloadPart
{
    long ExpectedSize { get; }
    long From { get; }
    string RangeHeader { get; }
    ContentRangeHeaderValue RangeHeaderValue { get; }
    long Size { get; }
    long To { get; }
}