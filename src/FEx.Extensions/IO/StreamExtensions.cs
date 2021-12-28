using System.Security.Cryptography;

namespace FEx.Extensions.IO;

public static class StreamExtensions
{
    public static void CopyStreamToStream(this Stream sourceStream, Stream destStream, Action<double> progressMaximumSet = null, Action<double> progressValueSet = null)
    {
        var buffer = new byte[BufferSize];
        var writtenBytes = 0;

        using (sourceStream)
        {
            if (sourceStream == null)
            {
                return;
            }

            progressMaximumSet?.BeginInvoke(sourceStream.Length, null, null);

            while (true)
            {
                int num = sourceStream.Read(buffer, 0, buffer.Length);
                int bytesRead;
                if ((bytesRead = num) != 0)
                {
                    destStream.Write(buffer, 0, bytesRead);
                    writtenBytes += num;
                    progressValueSet?.BeginInvoke(writtenBytes, null, null);
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
        using (input)
        using (MemoryStream ms = await input.ToMemoryStreamAsync())
        {
            return ms.ToArray();
        }
    }

    public static async Task<MemoryStream> ToMemoryStreamAsync(this Stream input)
    {
        using (input)
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
        {
            hash = md5Algorithm.ComputeHash(data);
        }

        return hash.GetHashString(removeDashes, toLower, asBase64String);
    }

    private static int BufferSize { get; } = 81920;
}