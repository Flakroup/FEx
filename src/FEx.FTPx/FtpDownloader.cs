using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Core.Abstractions.Extensions;
using FEx.MVVM;
using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Abstractions.Extensions;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.FTPx;

/// <summary>
/// FTP download utilities with resume support.
/// </summary>
public static class FtpDownloader
{
    /// <summary>Default cap on consecutive download attempts that make no progress.</summary>
    public const int DefaultMaxAttempts = 30;

    public static string? StatusDescription { get; set; }

    /// <summary>
    /// Downloads a file, resuming and retrying until it is complete.
    /// </summary>
    /// <param name="fileName">The local file path to download to.</param>
    /// <param name="serverUri">The URI of the file on the server.</param>
    /// <param name="viewModel">The progress aggregator that receives progress updates.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="maxAttempts">Maximum consecutive attempts without progress before giving up.</param>
    /// <param name="cancellationToken">Cancels the download, including the wait between attempts.</param>
    /// <returns><c>true</c> when the file is complete; <c>false</c> when attempts ran out without an error to report.</returns>
    /// <exception cref="IOException">Attempts ran out and the last attempt threw; the exception is the inner exception.</exception>
    /// <exception cref="OperationCanceledException">The download was cancelled.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxAttempts" /> is less than 1.</exception>
    public static Task<bool> DownloadFileAsync(string fileName,
                                               Uri serverUri,
                                               IProgressAggregator? viewModel,
                                               string username = "",
                                               string password = "",
                                               int maxAttempts = DefaultMaxAttempts,
                                               CancellationToken cancellationToken = default) =>
        DownloadFileAsync(FtpTransport.Instance,
            TimeSpan.FromSeconds(1),
            fileName,
            serverUri,
            viewModel,
            username,
            password,
            maxAttempts,
            cancellationToken);

    internal static async Task<bool> DownloadFileAsync(IFtpTransport transport,
                                                       TimeSpan retryDelay,
                                                       string fileName,
                                                       Uri serverUri,
                                                       IProgressAggregator? viewModel,
                                                       string username,
                                                       string password,
                                                       int maxAttempts,
                                                       CancellationToken cancellationToken)
    {
        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), maxAttempts, "At least one attempt is required.");

        var state = new FtpDownloadState();
        var attempts = 0;
        long lastLength = -1;
        Exception? lastError = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var offset = File.Exists(fileName) ? new FileInfo(fileName).Length : 0;

            // Progress resets the cap: it bounds attempts that get nowhere, not a long flaky transfer.
            if (offset > lastLength)
                attempts = 0;

            lastLength = offset;

            if (++attempts > maxAttempts)
            {
                if (lastError is not null)
                    throw new IOException($"FTP download of '{Redact(serverUri)}' failed after {maxAttempts} attempts.", lastError);

                return false;
            }

            try
            {
                if (await RestartDownloadFromServerAsync(transport,
                        state,
                        fileName,
                        serverUri,
                        viewModel,
                        offset,
                        username,
                        password,
                        cancellationToken))
                    return true;

                lastError = null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex;
                viewModel?.IfNotNull(v => v.SetStatusInfo(ex.Message));
            }

            await Task.Delay(retryDelay, cancellationToken);
        }
    }

    /// <summary>
    /// Restarts the download from server.
    /// </summary>
    /// <param name="fileName">Name of the file. Identifies the local file.</param>
    /// <param name="serverUri">The server URI. Identifies the remote file.</param>
    /// <param name="viewModel">The view model.</param>
    /// <param name="offset">The offset. Specifies where in the server file to start reading data.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="state">
    /// Retry bookkeeping. Pass the same instance on every call of a retry loop for one download so that repeated
    /// aborted reads escalate; omit it and each call starts fresh.
    /// </param>
    /// <param name="cancellationToken">Cancels the transfer.</param>
    /// <returns></returns>
    public static Task<bool> RestartDownloadFromServerAsync(string fileName,
                                                            Uri serverUri,
                                                            IProgressAggregator? viewModel,
                                                            long offset = 0,
                                                            string username = "",
                                                            string password = "",
                                                            FtpDownloadState? state = null,
                                                            CancellationToken cancellationToken = default) =>
        RestartDownloadFromServerAsync(FtpTransport.Instance,
            state ?? new FtpDownloadState(),
            fileName,
            serverUri,
            viewModel,
            offset,
            username,
            password,
            cancellationToken);

    internal static async Task<bool> RestartDownloadFromServerAsync(IFtpTransport transport,
                                                                    FtpDownloadState state,
                                                                    string fileName,
                                                                    Uri serverUri,
                                                                    IProgressAggregator? viewModel,
                                                                    long offset,
                                                                    string username,
                                                                    string password,
                                                                    CancellationToken cancellationToken)
    {
        if (serverUri.Scheme == Uri.UriSchemeFtp)
        {
            viewModel?.IfNotNull(v => v.SetStatusInfo(fileName));
            var fileSize = await transport.GetSizeAsync(serverUri, username, password, cancellationToken);
            long localFileSize;

            if (File.Exists(fileName))
            {
                localFileSize = new FileInfo(fileName).Length;

                if (localFileSize >= fileSize)
                    return true;
            }

            using var response = await transport.OpenAsync(serverUri, username, password, offset, cancellationToken);

            using var stream = response.GetResponseStream();
            viewModel?.PrgSetMax(fileSize - offset);
            viewModel?.IfNotNull(v => v.SetIsIndeterminate(true));

            var mode = File.Exists(fileName)
                ? FileMode.Append
                : FileMode.CreateNew;

            using var fs = new FileStream(fileName, mode);
            viewModel?.IfNotNull(v => v.SetStatusInfo($"Downloading: {fileName} "));

            try
            {
                if (stream is not null)
                {
                    var readCount = 1;
                    long prg = 0;
                    viewModel?.SetCurrentDownloadState(prg, fileSize - offset);
                    //viewModel?.ThreadsInfo = $"{(prg + offset) / (double)fileSize * 100}%";
                    const int cacheLength = 1024 * 1024 / 2;
                    var sw = new Stopwatch();

                    while (readCount > 0)
                    {
                        var cache = new List<byte>();
                        var pos = 0;

                        try
                        {
                            //viewModel?.ProgressIsIndeterminate = true;
                            byte[] buffer;
                            sw.Restart();

                            while (readCount > 0
                                   && pos < cacheLength)
                            {
                                var toRead = Math.Min(8192, cacheLength - pos);
                                buffer = new byte[toRead];
                                readCount = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                                cancellationToken.ThrowIfCancellationRequested();
                                state.RetryCount = 0;

                                if (readCount > 0)
                                {
                                    cache.AddRange(new ArraySegment<byte>(buffer, 0, readCount));
                                    pos += readCount;
                                    prg += readCount;
                                    viewModel?.PrgSet(prg);
                                }
                            }

                            sw.Stop();
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            //ex.HandleException( "", false);
                            viewModel?.IfNotNull(v => v.SetStatusInfo(ex.Message));

                            if (cache?.Count > 0)
                                await fs.WriteAsync([.. cache], 0, cache.Count, CancellationToken.None);

                            if (transport.IsLocalProcessingAbort(ex))
                            {
                                if (state.RetryCount >= FtpDownloadState.MaxReadRetries)
                                {
                                    var failedRetryCount = state.RetryCount;

                                    var detectedOffset = await DetectOffsetAsync(transport,
                                        serverUri,
                                        offset + prg,
                                        username,
                                        password,
                                        viewModel,
                                        cancellationToken);

                                    // Never zero-fill past the remote end: the probe steps in 512 KiB chunks.
                                    var newOffset = Math.Min(detectedOffset, fileSize);
                                    var buffer = new byte[Math.Max(0, newOffset - (offset + prg))];
                                    await fs.WriteAsync(buffer, 0, buffer.Length, CancellationToken.None);

                                    state.RetryCount = 0;

                                    await FExMvvm.MessagePopupService.ShowMessageAsync(
                                        $"{fileName} bytes at position {offset + prg + 1}-{newOffset} replaced with 0 due to {failedRetryCount} unsuccessful read attempts.\n",
                                        "Something wrong happened",
                                        MessageIcon.Exclamation,
                                        FExMessageButton.OK,
                                        null,
                                        true,
                                        false,
                                        null,
                                        LogLevel.Information,
                                        null);
                                }
                                else
                                {
                                    state.RetryCount++;
                                }
                            }

                            break;
                        }

                        await fs.WriteAsync([.. cache], 0, cache.Count, CancellationToken.None);
                        viewModel?.PrgSet(prg);
                        viewModel?.SetCurrentDownloadState(prg, fileSize - offset);
                        //viewModel?.ThreadsInfo = $"{(prg + offset) / (double)fileSize * 100}% {sw.GetTime()}";
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                //ex.HandleException( "", false);
                viewModel?.IfNotNull(v => v.SetStatusInfo(ex.Message));
            }

            viewModel?.IfNotNull(v => v.SetIsIndeterminate(true));
            StatusDescription = response.StatusDescription;
            fs.Close();
            stream?.Close();
            localFileSize = new FileInfo(fileName).Length;

            return File.Exists(fileName) && localFileSize >= fileSize;
        }

        return false;
    }

    private static string Redact(Uri uri) =>
        uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped);

    /// <summary>
    /// Calculates the size.
    /// </summary>
    /// <param name="serverUri">The server URI.</param>
    /// <param name="promptOnError">if set to <c>true</c> [prompt on error].</param>
    /// <param name="unit">The unit.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="client">The <see cref="HttpClient" /> used for http and https URIs; a default one is used when <see langword="null" />.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns></returns>
    public static async Task<double> CalculateSizeAsync(Uri serverUri,
                                                        bool promptOnError = true,
                                                        LengthType unit = LengthType.Megabytes,
                                                        string username = "",
                                                        string password = "",
                                                        HttpClient? client = null,
                                                        CancellationToken cancellationToken = default)
    {
        double bytesTotal = 0;

        try
        {
            if (serverUri.Scheme == Uri.UriSchemeHttp
                || serverUri.Scheme == Uri.UriSchemeHttps)
            {
                bytesTotal = Math.Max(await serverUri.GetHttpFileSizeAsync(client: client, cancellationToken: cancellationToken), 0);
            }
            else if (serverUri.Scheme == Uri.UriSchemeFtp)
            {
                var request = (FtpWebRequest)serverUri.GetWebRequest();
                request.Proxy = null;
                request.ApplyCredentials(username, password);
                request.Method = WebRequestMethods.Ftp.GetFileSize;

                using var response = (FtpWebResponse)await request.GetResponseAsync();
                bytesTotal = response.ContentLength;
            }
        }
        catch (Exception ex)
        {
            ex.HandleException(promptOnError);
        }

        return unit == LengthType.Bytes
            ? bytesTotal
            : FileLengthConverter.ConvertFileLength(bytesTotal, LengthType.Bytes, unit).length;
    }

    private static async Task<long> DetectOffsetAsync(IFtpTransport transport,
                                                      Uri serverUri,
                                                      long offset,
                                                      string username,
                                                      string password,
                                                      IProgressAggregator? viewModel,
                                                      CancellationToken cancellationToken)
    {
        var newOffset = offset;

        if (serverUri.Scheme == Uri.UriSchemeFtp)
        {
            var fileSize = await transport.GetSizeAsync(serverUri, username, password, cancellationToken);
            viewModel?.PrgSetMax(fileSize - offset);
            var readCount = 0;

            while (readCount <= 0
                   && newOffset < fileSize)
            {
                using var response = await transport.OpenAsync(serverUri, username, password, offset, cancellationToken);

                try
                {
                    using var stream = response.GetResponseStream();

                    if (stream is not null)
                    {
                        var buffer = new byte[1];
                        readCount = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        newOffset--;

                        while (readCount > 0)
                        {
                            using var innerResponse = await transport.OpenAsync(serverUri, username, password, offset, cancellationToken);

                            using var innerStream = innerResponse.GetResponseStream();

                            if (innerStream is not null)
                            {
                                buffer = new byte[1];
                                readCount = await innerStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                                viewModel?.PrgSet(newOffset - offset);
                                viewModel?.SetCurrentDownloadState(newOffset - offset, fileSize - offset);
                                newOffset--;
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    ex.HandleException();

                    return newOffset;
                }

                viewModel?.PrgSet(newOffset - offset);
                viewModel?.SetCurrentDownloadState(newOffset - offset, fileSize - offset);

                if (readCount <= 0)
                    newOffset += 1024 * 1024 / 2;
            }
        }

        return newOffset;
    }
}