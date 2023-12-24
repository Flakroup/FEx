using FEx.Extensions.Base.Helpers;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Extensions.IO;

public static class StreamExtensions
{
    private static int BufferSize { get; } = 81920;

    public static async Task CopyStreamToStreamAsync(this Stream sourceStream,
                                                     Stream destStream,
                                                     Action<double> progressMaximumSet = null,
                                                     Action<double> progressValueSet = null,
                                                     long? length = null,
                                                     CancellationToken cancellationToken = default)
    {
#if NETSTANDARD
        var buffer = new byte[BufferSize];
#else
        var buffer = new Memory<byte>(new byte[BufferSize]);
#endif
        var writtenBytes = 0;

        sourceStream.Guard(nameof(sourceStream));

#if NETSTANDARD
#pragma warning disable IDISP007
        using Stream stream = sourceStream;
#pragma warning restore IDISP007
#else
#pragma warning disable IDISP007
        await using Stream stream = sourceStream;
#pragma warning restore IDISP007
#endif
        progressMaximumSet?.BeginInvoke(stream.Length, null, null);

        while (true)
        {
#if NETSTANDARD
            int num = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
#else
            int num = await stream.ReadAsync(buffer, cancellationToken);
#endif
#if NETSTANDARD
            int bytesRead;

            if ((bytesRead = num) != 0)
            {
                await destStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
#else
            if (num != 0)
            {
                await destStream.WriteAsync(buffer, cancellationToken);
#endif
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

        sourceStream.Guard(nameof(sourceStream));

#pragma warning disable IDISP007
        using Stream stream = sourceStream;
#pragma warning restore IDISP007

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
        //todo refactor it
#if NETSTANDARD
#pragma warning disable IDISP007
        using (input)
#pragma warning restore IDISP007
        using (MemoryStream ms = await input.ToMemoryStreamAsync())
#else
#pragma warning disable IDISP007
        await using (input)
#pragma warning restore IDISP007
        await using (MemoryStream ms = await input.ToMemoryStreamAsync())
#endif
            return ms.ToArray();
    }

    public static async Task<MemoryStream> ToMemoryStreamAsync(this Stream input)
    {
#if NETSTANDARD
#pragma warning disable IDISP007
        using Stream stream = input;
#pragma warning restore IDISP007
#else
#pragma warning disable IDISP007
        await using Stream stream = input;
#pragma warning restore IDISP007
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