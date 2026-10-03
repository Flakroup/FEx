using FEx.Agnostics.Abstractions.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Downloader.Extensions;

public static class HttpResponseMessageExtensions
{
    private const int BufferSize = 81920;

    public static async Task<Stream> ReadContentStreamAsync(this HttpResponseMessage response,
                                                            CancellationToken cancellationToken = default) =>
#if NET
        await response.Content.ReadAsStreamAsync(cancellationToken);
#else
        await response.Content.ReadAsStreamAsync();
#endif

    /// <summary>
    /// Streams the response body to <paramref name="filePath" />. The body goes to a temporary file next to the target
    /// and replaces it only once it was received completely, so a failed, cancelled or truncated download never leaves a
    /// corrupt target behind.
    /// </summary>
    /// <param name="response">A response read with <see cref="HttpCompletionOption.ResponseHeadersRead" />.</param>
    /// <param name="filePath">The target file.</param>
    /// <param name="onProgress">Receives the bytes received so far and the expected total (null when unknown).</param>
    /// <param name="cancellationToken">Cancels the transfer.</param>
    /// <exception cref="HttpStatusException">The response status is not a success status.</exception>
    public static async Task DownloadToFileAsync(this HttpResponseMessage response,
                                                 string filePath,
                                                 Action<long, long?>? onProgress = null,
                                                 CancellationToken cancellationToken = default)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpStatusException(response.StatusCode, response.RequestMessage?.RequestUri, response.ReasonPhrase);

        var dirPath = Path.GetDirectoryName(Path.GetFullPath(filePath))
                      ?? throw new InvalidOperationException("Target directory path is null.");
        Directory.CreateDirectory(dirPath);

        var expected = response.Content.Headers.ContentLength;
        var tempPath = Path.Combine(dirPath, $"{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");
        long received = 0;

        try
        {
            using (var source = await response.ReadContentStreamAsync(cancellationToken))
            using (var target = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, true))
            {
                var buffer = new byte[BufferSize];
                int read;

                while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await target.WriteAsync(buffer, 0, read, cancellationToken);
                    received += read;
                    onProgress?.Invoke(received, expected);
                }
            }

            if (expected is { } length
                && received != length)
                throw new IOException($"Download was truncated: received {received} of {length} bytes.");

            if (File.Exists(filePath))
                File.Replace(tempPath, filePath, null);
            else
                File.Move(tempPath, filePath);
        }
        catch
        {
            File.Delete(tempPath);

            throw;
        }
    }
}
