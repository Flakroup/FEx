using FEx.Agnostics.Abstractions.Helpers;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions for copying, buffering and hashing streams.</summary>
public static class StreamExtensions
{
    private static int BufferSize { get; } = 81920;

    /// <summary>Copies a stream to another stream asynchronously and disposes the source when done.</summary>
    /// <param name="sourceStream">The stream to read from.</param>
    /// <param name="destStream">The stream to write to.</param>
    /// <param name="progressMaximumSet">Called once with the length of the source stream.</param>
    /// <param name="progressValueSet">Called after each chunk with the total number of bytes written.</param>
    /// <param name="length">When set, copying stops once this many bytes have been written.</param>
    /// <param name="cancellationToken">Token used to cancel the copy.</param>
    public static async Task CopyStreamToStreamAsync(this Stream sourceStream,
                                                     Stream destStream,
                                                     Action<double>? progressMaximumSet = null,
                                                     Action<double>? progressValueSet = null,
                                                     long? length = null,
                                                     CancellationToken cancellationToken = default)
    {
#if NETSTANDARD
        var buffer = new byte[BufferSize];
#else
        var buffer = new Memory<byte>(new byte[BufferSize]);
#endif
        var writtenBytes = 0L;

        sourceStream.Guard(nameof(sourceStream));

#if NETSTANDARD2_0
#pragma warning disable IDISP007
        using var stream = sourceStream;
#pragma warning restore IDISP007
#else
#pragma warning disable IDISP007
        await using var stream = sourceStream;
#pragma warning restore IDISP007
#endif
        progressMaximumSet?.Invoke(stream.Length);

        while (true)
        {
#if NETSTANDARD
            var num = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
#else
            var num = await stream.ReadAsync(buffer, cancellationToken);
#endif
#if NETSTANDARD
            int bytesRead;

            if ((bytesRead = num) != 0)
            {
                await destStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
#else
            if (num != 0)
            {
                await destStream.WriteAsync(buffer.Slice(0, num), cancellationToken);
#endif
                writtenBytes += num;
                progressValueSet?.Invoke(writtenBytes);

                if (writtenBytes == length)
                    break;
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>Copies a stream to another stream and disposes the source when done.</summary>
    /// <param name="sourceStream">The stream to read from.</param>
    /// <param name="destStream">The stream to write to.</param>
    /// <param name="progressMaximumSet">Called once with the length of the source stream.</param>
    /// <param name="progressValueSet">Called after each chunk with the total number of bytes written.</param>
    /// <param name="length">When set, copying stops once this many bytes have been written.</param>
    public static void CopyStreamToStream(this Stream sourceStream,
                                          Stream destStream,
                                          Action<double>? progressMaximumSet = null,
                                          Action<double>? progressValueSet = null,
                                          long? length = null)
    {
        var buffer = new byte[BufferSize];
        var writtenBytes = 0L;

        sourceStream.Guard(nameof(sourceStream));

#pragma warning disable IDISP007
        using var stream = sourceStream;
#pragma warning restore IDISP007

        progressMaximumSet?.Invoke(stream.Length);

        while (true)
        {
            var num = stream.Read(buffer, 0, buffer.Length);
            int bytesRead;

            if ((bytesRead = num) != 0)
            {
                destStream.Write(buffer, 0, bytesRead);
                writtenBytes += num;
                progressValueSet?.Invoke(writtenBytes);

                if (writtenBytes == length)
                    break;
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>Reads a stream to the end and disposes it.</summary>
    /// <param name="input">The stream to read.</param>
    /// <returns>All bytes of the stream.</returns>
    public static async Task<byte[]> ReadFullyAsync(this Stream input)
    {
        //todo refactor it
#if NETSTANDARD2_0
#pragma warning disable IDISP007
        using (input)
#pragma warning restore IDISP007
        using (var ms = await input.CopyToMemoryStreamAsync(true))
#else
#pragma warning disable IDISP007
        await using (input)
#pragma warning restore IDISP007
        await using (var ms = await input.CopyToMemoryStreamAsync(true))
#endif
            // A non-null, non-cancellable source (no token passed) never yields a null stream here.
            return ms!.ToArray();
    }

    /// <summary>Copies a stream into a new memory stream positioned at the start.</summary>
    /// <param name="streamToCopy">The stream to copy; rewound first when it supports seeking.</param>
    /// <param name="disposeSource">Whether to dispose the source after copying.</param>
    /// <param name="cancellationToken">Token used to cancel the copy.</param>
    /// <returns>The memory stream, or null when the source is null or the copy was canceled.</returns>
    public static async Task<MemoryStream?> CopyToMemoryStreamAsync(this Stream streamToCopy,
                                                                   bool disposeSource = false,
                                                                   CancellationToken cancellationToken = default)
    {
        const int defaultBufferSize = 81920;

        try
        {
            if (streamToCopy is null)
                return null;

            if (streamToCopy.CanSeek)
                streamToCopy.Seek(0, SeekOrigin.Begin);

            var stream = new MemoryStream();
            await streamToCopy.CopyToAsync(stream, defaultBufferSize, cancellationToken);
            stream.Seek(0, SeekOrigin.Begin);

            if (disposeSource)
#pragma warning disable IDISP007
#if NETSTANDARD2_0
                streamToCopy.Dispose();
#else
                await streamToCopy.DisposeAsync();
#endif
#pragma warning restore IDISP007

            return stream;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    /// <summary>Copies a stream into a new memory stream positioned at the start.</summary>
    /// <param name="streamToCopy">The stream to copy; rewound first when it supports seeking.</param>
    /// <param name="disposeSource">Whether to dispose the source after copying.</param>
    /// <returns>The memory stream, or null when the source is null.</returns>
    public static MemoryStream? CopyToMemoryStream(this Stream streamToCopy, bool disposeSource = false)
    {
        const int defaultBufferSize = 81920;

        if (streamToCopy is null)
            return null;

        if (streamToCopy.CanSeek)
            streamToCopy.Seek(0, SeekOrigin.Begin);

        var stream = new MemoryStream();
        streamToCopy.CopyTo(stream, defaultBufferSize);
        stream.Seek(0, SeekOrigin.Begin);

        if (disposeSource)
#pragma warning disable IDISP007
            streamToCopy.Dispose();
#pragma warning restore IDISP007

        return stream;
    }

    /// <summary>Copies a stream into a new memory stream without disposing the source.</summary>
    /// <param name="streamToCopy">The stream to copy.</param>
    /// <param name="cancellationToken">Token used to cancel the copy.</param>
    /// <returns>The memory stream, or null when the source is null or the copy was canceled.</returns>
    public static async Task<Stream?> CopyToStreamAsync(this Stream streamToCopy,
                                                       CancellationToken cancellationToken = default) =>
        await streamToCopy.CopyToMemoryStreamAsync(false, cancellationToken);

    /// <summary>Computes the MD5 hash of a stream.</summary>
    /// <param name="data">The stream to hash.</param>
    /// <param name="removeDashes">Whether to remove the dashes between hex bytes.</param>
    /// <param name="toLower">Whether to lower-case the hex text.</param>
    /// <param name="asBase64String">Whether to return the hash as Base64 instead of hex.</param>
    /// <returns>The formatted hash.</returns>
    public static string ComputeMd5Hash(this Stream data,
                                        bool removeDashes = true,
                                        bool toLower = true,
                                        bool asBase64String = false)
    {
#if NETSTANDARD
        byte[] hash;

        using (var md5Algorithm = MD5.Create())
            hash = md5Algorithm.ComputeHash(data);
#else
        var hash = MD5.HashData(data); //todo provide async overloads for NET
#endif
        return hash.GetHashString(removeDashes, toLower, asBase64String);
    }
}