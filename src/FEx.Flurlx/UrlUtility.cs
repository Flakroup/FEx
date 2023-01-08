using FEx.Extensions.Helpers;
using FEx.Extensions.Numericals;
using Flurl;
using Flurl.Http;
using System.Threading.Tasks;

namespace FEx.Flurlx;

public static class UrlUtility
{
    public static async Task<double> CalculateSizeAsync(this Url url, LengthType unit = LengthType.Megabytes, IFlurlClient client = null)
    {
        const string contentLenthKey = "Content-Length";
        var dispose = false;

        try
        {
            if (client is null)
            {
                client = new FlurlClient();
                dispose = true;
            }

            IFlurlResponse r = await client.Request(url)
                .HeadAsync();
            string contentLength = r.Headers.FirstOrDefault(contentLenthKey);
            double bytesTotal = contentLength.FromString();
            if (unit == LengthType.Bytes)
                return bytesTotal;

            return FileLengthConverter.ConvertFileLength(bytesTotal, LengthType.Bytes, unit)
                .length;
        }
        finally
        {
            if (dispose)
                client?.Dispose();
        }
    }
}