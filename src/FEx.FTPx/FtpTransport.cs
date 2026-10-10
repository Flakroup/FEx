using FluentFTP;
using FluentFTP.Exceptions;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.FTPx;

/// <summary>Seam over the FTP wire calls so <see cref="FtpDownloader" /> can be tested without a server.</summary>
internal interface IFtpTransport
{
    /// <summary>Remote file size in bytes. Throws when the server cannot be asked; never reports 0 for a failure.</summary>
    Task<long> GetSizeAsync(Uri serverUri, string username, string password, CancellationToken cancellationToken);

    /// <summary>Opens the file at <paramref name="offset" />. Throws when the server cannot be reached or refuses.</summary>
    Task<IFtpResponse> OpenAsync(Uri serverUri,
                                 string username,
                                 string password,
                                 long offset,
                                 CancellationToken cancellationToken);

    bool IsLocalProcessingAbort(Exception exception);
}

/// <summary>An open FTP download response; disposing it releases the underlying connection. Disposing twice is harmless.</summary>
internal interface IFtpResponse : IAsyncDisposable
{
    string StatusDescription { get; }

    Stream? GetResponseStream();
}

/// <summary>Downloads through FluentFTP, with every connection taken from the <see cref="FtpClientFactory" /> of the host.</summary>
internal sealed class FtpTransport : IFtpTransport
{
    private const string LocalProcessingAbortCode = "451";

    public static FtpTransport Instance { get; } = new();

    public async Task<long> GetSizeAsync(Uri serverUri,
                                         string username,
                                         string password,
                                         CancellationToken cancellationToken)
    {
#pragma warning disable IDISP001 // released through FtpCommon.ReleaseAsync on every path
        var client = await CreateClientAsync(serverUri, username, password);
#pragma warning restore IDISP001

        try
        {
            await client.Connect(cancellationToken);
            var size = await client.GetFileSize(RemotePath(serverUri), -1, cancellationToken);

            return size >= 0 ? size : throw new FtpException("The server did not report the size of the file.");
        }
        finally
        {
            await FtpCommon.ReleaseAsync(client);
        }
    }

    public async Task<IFtpResponse> OpenAsync(Uri serverUri,
                                              string username,
                                              string password,
                                              long offset,
                                              CancellationToken cancellationToken)
    {
#pragma warning disable IDISP001 // released through FtpCommon.ReleaseAsync on every path
        var client = await CreateClientAsync(serverUri, username, password);
#pragma warning restore IDISP001

        try
        {
            await client.Connect(cancellationToken);

            // fileLen -1: the size is not needed here, so no SIZE round trip.
            return await client.OpenRead(RemotePath(serverUri), FtpDataType.Binary, offset, -1L, cancellationToken) is FtpDataStream data
                ? new Response(client, data)
                : throw new FtpException("The server did not open a data connection.");
        }
        catch
        {
            await FtpCommon.ReleaseAsync(client);

            throw;
        }
    }

    /// <summary>A 451 reply to the transfer: the server gave up on a read, which the downloader retries and, past a limit, skips.</summary>
    public bool IsLocalProcessingAbort(Exception exception) =>
        exception is FtpCommandException { CompletionCode: LocalProcessingAbortCode };

    /// <summary>Explicit credentials win when both parts are given; otherwise the Uri user info, otherwise anonymous.</summary>
    internal static NetworkCredential? GetCredentials(Uri serverUri, string username, string password)
    {
        if (!string.IsNullOrWhiteSpace(username)
            && !string.IsNullOrWhiteSpace(password))
            return new(username, password);

        if (serverUri.UserInfo.Length == 0)
            return null;

        var parts = serverUri.UserInfo.Split([':'], 2);

        return new(Uri.UnescapeDataString(parts[0]), parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty);
    }

    /// <summary>Path relative to the login directory, as FTP URLs conventionally read; an absolute path is spelled <c>//dir</c> or <c>%2Fdir</c>.</summary>
    internal static string RemotePath(Uri serverUri)
    {
        var path = Uri.UnescapeDataString(serverUri.AbsolutePath);

        return path.StartsWith("/", StringComparison.Ordinal) ? path.Substring(1) : path;
    }

    private static async Task<AsyncFtpClient> CreateClientAsync(Uri serverUri, string username, string password)
    {
        // One pool per host: a path or user info in the Uri must not give every file its own connection limit.
        var host = new UriBuilder(serverUri.Scheme, serverUri.DnsSafeHost, serverUri.Port).Uri;
        var factory = await FtpClientFactory.GetInstanceAsync(host.AbsoluteUri);

        return await factory.CreateAsync(GetCredentials(serverUri, username, password), false);
    }

    private sealed class Response(AsyncFtpClient client, FtpDataStream data) : IFtpResponse
    {
        private readonly DownloadStream _stream = new(data);
        private int _disposed;

        public string StatusDescription { get; } = $"{data.CommandStatus.Code} {data.CommandStatus.Message}".Trim();

        public Stream GetResponseStream() => _stream;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            try
            {
                await _stream.CompleteAsync();
            }
            catch (Exception ex) when (ex is FtpException or IOException or TimeoutException)
            {
                // Abandoned or broken transfer: the release below drops the connection anyway.
            }
            finally
            {
                await FtpCommon.ReleaseAsync(client);
            }
        }
    }

    /// <summary>
    /// Reads the data connection and, at its end, reads the final reply of the server, so a transfer the server
    /// aborted (451) fails the read that hit the end instead of looking like a complete file.
    /// </summary>
    private sealed class DownloadStream(FtpDataStream data) : Stream
    {
        private bool _completed;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public async Task CompleteAsync()
        {
            if (_completed)
                return;

            _completed = true;
            await data.CloseAsync();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = await data.ReadAsync(buffer, offset, count, cancellationToken);

            if (read == 0)
                await CompleteAsync();

            return read;
        }

        // ponytail: the downloader reads asynchronously only; a synchronous read would have to block on the final reply.
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("Read asynchronously.");

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
