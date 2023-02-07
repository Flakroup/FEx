using FEx.Extensions.Helpers;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FEx.Extensions.IO;

public static class StreamExtensions
{
    private static int BufferSize { get; } = 81920;

    public static async Task CopyStreamToStreamAsync(this Stream sourceStream, Stream destStream, Action<double> progressMaximumSet = null, Action<double> progressValueSet = null, long? length = null)
    {
        var buffer = new byte[BufferSize];
        var writtenBytes = 0;

        await using (sourceStream)
        {
            if (sourceStream == null)
                return;

            progressMaximumSet?.BeginInvoke(sourceStream.Length, null, null);

            while (true)
            {
                int num = await sourceStream.ReadAsync(buffer, 0, buffer.Length);
                int bytesRead;
                if ((bytesRead = num) != 0)
                {
                    await destStream.WriteAsync(buffer, 0, bytesRead);
                    writtenBytes += num;
                    progressValueSet?.BeginInvoke(writtenBytes, null, null);
                    if (writtenBytes == length)
                        break;
                }
                else
                {
                    break;
                }
            }
        }
    }

    public static async Task<byte[]> ReadFullyAsync(this Stream input)
    {
        await using (input) //todo refactor it
        await using (MemoryStream ms = await input.ToMemoryStreamAsync())
            return ms.ToArray();
    }

    public static async Task<MemoryStream> ToMemoryStreamAsync(this Stream input)
    {
        await using (input)
        {
            var ms = new MemoryStream();
            await input.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }

    public static string ComputeMd5Hash(this Stream data, bool removeDashes = true, bool toLower = true, bool asBase64String = false)
    {
        byte[] hash;

        using (var md5Algorithm = MD5.Create())
            hash = md5Algorithm.ComputeHash(data);

        return hash.GetHashString(removeDashes, toLower, asBase64String);
    }
}