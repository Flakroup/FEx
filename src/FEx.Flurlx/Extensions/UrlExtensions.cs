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
    public static async Task<double> CalculateSizeAsync(this Url url,
                                                        LengthType unit = LengthType.Megabytes,
                                                        IFlurlClient client = null)
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

    public static async Task<MemoryStream> GetBytesAsync(this Url url,
                                                         IFlurlClient client = null,
                                                         SeekOrigin origin = SeekOrigin.Begin,
                                                         long offset = 0,
                                                         long? length = null)
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
#if NETSTANDARD
                    using var rangedStream = await request.GetStreamAsync();
#else
                    await using var rangedStream = await request.GetStreamAsync();
#endif
                    await rangedStream.CopyToAsync(ms);

                    return ms;
                }

#if NETSTANDARD
                using var seekableStream = await request.GetStreamAsync();
#else
                await using var seekableStream = await request.GetStreamAsync();
#endif
                seekableStream.Seek(offset, origin);
                await seekableStream.CopyStreamToStreamAsync(ms, length: length);

                return ms;
            }

#if NETSTANDARD
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