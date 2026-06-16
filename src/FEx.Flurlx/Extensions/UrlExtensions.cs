using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Numericals;
using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Agnostics.Abstractions.Utilities;
using Flurl;
using Flurl.Http;
using System.IO;
using System.Threading.Tasks;

namespace FEx.Flurlx.Extensions;

public static class UrlExtensions
{
    public static Task<double> CalculateSizeAsync(this Url url) => url.CalculateSizeAsync(LengthType.Megabytes, null);

    public static Task<double> CalculateSizeAsync(this Url url, LengthType unit) => url.CalculateSizeAsync(unit, null);

    public static async Task<double> CalculateSizeAsync(this Url url, LengthType unit, IFlurlClient client)
    {
        var dispose = false;

        try
        {
            if (client is null)
            {
#pragma warning disable IDISP001
                client = new FlurlClient();
#pragma warning restore IDISP001
                dispose = true;
            }

            using var response = await client.Request(url).HeadAsync();
            var bytesTotal = GetContentLength(response);

            return unit == LengthType.Bytes
                ? bytesTotal
                : FileLengthConverter.ConvertFileLength(bytesTotal, LengthType.Bytes, unit).length;
        }
        finally
        {
            if (dispose)
                client.Dispose();
        }
    }

    public static Task<MemoryStream> GetBytesAsync(this Url url) => url.GetBytesAsync(null, SeekOrigin.Begin, 0, null);

    public static Task<MemoryStream> GetBytesAsync(this Url url, IFlurlClient client) =>
        url.GetBytesAsync(client, SeekOrigin.Begin, 0, null);

    public static Task<MemoryStream> GetBytesAsync(this Url url, IFlurlClient client, SeekOrigin origin) =>
        url.GetBytesAsync(client, origin, 0, null);

    public static Task<MemoryStream> GetBytesAsync(this Url url, IFlurlClient client, SeekOrigin origin, long offset) =>
        url.GetBytesAsync(client, origin, offset, null);

    public static async Task<MemoryStream> GetBytesAsync(this Url url,
                                                         IFlurlClient client,
                                                         SeekOrigin origin,
                                                         long offset,
                                                         long? length)
    {
        const string acceptRangesHeader = "Accept-Ranges";
        var dispose = false;

        try
        {
            if (client is null)
            {
#pragma warning disable IDISP001
                client = new FlurlClient();
#pragma warning restore IDISP001
                dispose = true;
            }

            var ms = new MemoryStream();
            var request = client.Request(url);

            if (length.HasValue)
            {
                using var response = await request.HeadAsync();

                var headers = response.ResponseMessage.GetAllHeaders();

                if (headers.ContainsKey(acceptRangesHeader))
                {
                    var bytesTotal = GetContentLength(response);

                    var fromBytes = origin == SeekOrigin.Begin
                        ? offset
                        : bytesTotal - offset;

                    var toBytes = fromBytes + length;
                    request = request.WithHeader("Range", $"bytes={fromBytes}-{toBytes}");
#if NETSTANDARD2_0
                    using var rangedStream = await request.GetStreamAsync();
#else
                    await using var rangedStream = await request.GetStreamAsync();
#endif
                    await rangedStream.CopyToAsync(ms);

                    return ms;
                }

#if NETSTANDARD2_0
                using var seekableStream = await request.GetStreamAsync();
#else
                await using var seekableStream = await request.GetStreamAsync();
#endif
                seekableStream.Seek(offset, origin);
                await seekableStream.CopyStreamToStreamAsync(ms, length: length);

                return ms;
            }

#if NETSTANDARD2_0
            using var stream = await request.GetStreamAsync();
#else
            await using var stream = await request.GetStreamAsync();
#endif
            await stream.CopyToAsync(ms);

            return ms;
        }
        finally
        {
            if (dispose)
                client.Dispose();
        }
    }

    private static double GetContentLength(IFlurlResponse response)
    {
        const string contentLengthKey = "Content-Length";
        var contentLength = response.Headers.FirstOrDefault(contentLengthKey);

        return contentLength.ToDouble();
    }
}