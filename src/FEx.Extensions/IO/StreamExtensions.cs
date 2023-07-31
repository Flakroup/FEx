using FEx.Extensions.Helpers;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FEx.Extensions.IO;

public static class StreamExtensions
{
    private static int BufferSize { get; } = 81920;

    public static async Task CopyStreamToStreamAsync(this Stream sourceStream,
                                                     Stream destStream,
                                                     Action<double> progressMaximumSet = null,
                                                     Action<double> progressValueSet = null,
                                                     long? length = null)
    {
        var buffer = new byte[BufferSize];
        var writtenBytes = 0;

        if (sourceStream is null)
            return;

#if NETSTANDARD
        using Stream stream = sourceStream;
#else
        await using Stream stream = sourceStream;
#endif
        progressMaximumSet?.BeginInvoke(stream.Length, null, null);

        while (true)
        {
            int num = await stream.ReadAsync(buffer, 0, buffer.Length);
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

    public static void CopyStreamToStream(this Stream sourceStream,
                                          Stream destStream,
                                          Action<double> progressMaximumSet = null,
                                          Action<double> progressValueSet = null,
                                          long? length = null)
    {
        var buffer = new byte[BufferSize];
        var writtenBytes = 0;

        if (sourceStream is null)
            return;

        using Stream stream = sourceStream;
        progressMaximumSet?.BeginInvoke(stream.Length, null, null);

        while (true)
        {
            int num = stream.Read(buffer, 0, buffer.Length);
            int bytesRead;
            if ((bytesRead = num) != 0)
            {
                destStream.Write(buffer, 0, bytesRead);
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

    public static async Task<byte[]> ReadFullyAsync(this Stream input)
    {
#if NETSTANDARD
        using (input) //todo refactor it
        using (MemoryStream ms = await input.ToMemoryStreamAsync())
#else
        await using (input) //todo refactor it
        await using (MemoryStream ms = await input.ToMemoryStreamAsync())
#endif
            return ms.ToArray();
    }

    public static async Task<MemoryStream> ToMemoryStreamAsync(this Stream input)
    {
#if NETSTANDARD
        using Stream stream = input;
#else
        await using Stream stream = input;
#endif
        var ms = new MemoryStream();
        await input.CopyToAsync(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    public static string ComputeMd5Hash(this Stream data,
                                        bool removeDashes = true,
                                        bool toLower = true,
                                        bool asBase64String = false)
    {
        byte[] hash;

        using (var md5Algorithm = MD5.Create())
            hash = md5Algorithm.ComputeHash(data);

        return hash.GetHashString(removeDashes, toLower, asBase64String);
    }
}