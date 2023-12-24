using FEx.Extensions.Base.Converters;
using FEx.Extensions.Base.Enums;
using FEx.Extensions.IO;
using FEx.Extensions.Numericals;
using FEx.Extensions.Web;
using Flurl;
using Flurl.Http;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FEx.Flurlx;

public static class UrlUtility
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

            using IFlurlResponse response = await client.Request(url).HeadAsync();
            double bytesTotal = GetContentLength(response);

            return unit == LengthType.Bytes
                ? bytesTotal
                : FileLengthConverter.ConvertFileLength(bytesTotal, LengthType.Bytes, unit).length;
        }
        finally
        {
            if (dispose)
                client?.Dispose();
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
            IFlurlRequest request = client.Request(url);

            if (length.HasValue)
            {
                using IFlurlResponse response = await request.HeadAsync();

                Dictionary<string, string[]> headers = response.ResponseMessage.GetAllHeaders();

                if (headers.ContainsKey(acceptRangesHeader))
                {
                    double bytesTotal = GetContentLength(response);

                    double fromBytes = origin == SeekOrigin.Begin
                        ? offset
                        : bytesTotal - offset;

                    double? toBytes = fromBytes + length;
                    request = request.WithHeader("Range", $"bytes={fromBytes}-{toBytes}");
#if NETSTANDARD
                    using Stream rangedStream = await request.GetStreamAsync();
#else
                    await using Stream rangedStream = await request.GetStreamAsync();
#endif
                    await rangedStream.CopyToAsync(ms);

                    return ms;
                }

#if NETSTANDARD
                using Stream seekableStream = await request.GetStreamAsync();
#else
                await using Stream seekableStream = await request.GetStreamAsync();
#endif
                seekableStream.Seek(offset, origin);
                await seekableStream.CopyStreamToStreamAsync(ms, length: length);

                return ms;
            }

#if NETSTANDARD
            using Stream stream = await request.GetStreamAsync();
#else
            await using Stream stream = await request.GetStreamAsync();
#endif
            await stream.CopyToAsync(ms);

            return ms;
        }
        finally
        {
            if (dispose)
                client?.Dispose();
        }
    }

    private static double GetContentLength(IFlurlResponse response)
    {
        const string contentLengthKey = "Content-Length";
        string contentLength = response.Headers.FirstOrDefault(contentLengthKey);

        return contentLength.FromString();
    }
}