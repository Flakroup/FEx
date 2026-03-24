using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Core.Abstractions.Extensions;
using FEx.Downloader.Clients;
using FEx.MVVM;
using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Abstractions.Extensions;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Extensions;
using Microsoft.Extensions.Logging;
using FEx.Webx.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.FTPx;

/// <summary>
/// FTP download utilities with resume support.
/// </summary>
public static class FtpDownloader
{
    private static int _retryCount;

    public static string StatusDescription { get; set; }

    public static async Task<bool> DownloadFileAsync(string fileName,
                                                     Uri serverUri,
                                                     IProgressAggregator viewModel,
                                                     string username = "",
                                                     string password = "")
    {
        var res = false;

        try
        {
            long offset = 0;

            while (!res)
            {
                if (File.Exists(fileName))
                    offset = new FileInfo(fileName).Length;

                try
                {
                    var offset1 = offset;

                    res = await RestartDownloadFromServerAsync(fileName,
                        serverUri,
                        viewModel,
                        offset1,
                        username,
                        password);
                }
                catch (Exception ex)
                {
                    //ex.HandleException( "", false);
                    viewModel?.IfNotNull(v => v.SetStatusInfo(ex.Message));
                }

                await Task.Delay(1000);
            }
        }
        catch (Exception ex)
        {
            //ex.HandleException();
            viewModel?.IfNotNull(v => v.SetStatusInfo(ex.Message));
            res = false;
        }

        return res;
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
    /// <returns></returns>
    public static async Task<bool> RestartDownloadFromServerAsync(string fileName,
                                                                  Uri serverUri,
                                                                  IProgressAggregator viewModel,
                                                                  long offset = 0,
                                                                  string username = "",
                                                                  string password = "")
    {
        if (serverUri.Scheme == Uri.UriSchemeFtp)
        {
            viewModel?.IfNotNull(v => v.SetStatusInfo(fileName));
            var fileSize = (long)await CalculateSizeAsync(serverUri, false, LengthType.Bytes, username, password);
            long localFileSize;

            if (File.Exists(fileName))
            {
                localFileSize = new FileInfo(fileName).Length;

                if (localFileSize >= fileSize)
                    return true;
            }

            var resp = await TryGetResponseAsync(serverUri, username, password, offset);
            var response = resp.Value;
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
                                readCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                                _retryCount = 0;

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
                        catch (Exception ex)
                        {
                            //ex.HandleException( "", false);
                            viewModel?.IfNotNull(v => v.SetStatusInfo(ex.Message));

                            if (cache?.Count > 0)
                                await fs.WriteAsync([.. cache], 0, cache.Count);

                            var webEx = ex as WebException;

                            if (webEx is not null)
                            {
                                var ftpResponse = (FtpWebResponse)webEx.Response;

                                if (ftpResponse.StatusCode == FtpStatusCode.ActionAbortedLocalProcessingError)
                                {
                                    if (_retryCount >= 10)
                                    {
                                        var failedRetryCount = _retryCount;

                                        var newOffset = await DetectOffsetAsync(serverUri,
                                            offset + prg,
                                            username,
                                            password,
                                            viewModel);

                                        var buffer = new byte[newOffset - (offset + prg)];
                                        await fs.WriteAsync(buffer, 0, buffer.Length);

                                        _retryCount = 0;

                                        await FExMvvm.MessagePopupService.ShowMessageAsync(
                                            $"{fileName} bytes at position {offset + prg + 1}-{newOffset} replaced with 0 due to {failedRetryCount} unsuccessful read attempts.\n",
                                            "Something wrong happened", MessageIcon.Exclamation, FExMessageButton.OK, null, true, false, null, LogLevel.Information, null);
                                    }
                                    else
                                    {
                                        _retryCount++;
                                    }
                                }
                            }

                            break;
                        }

                        await fs.WriteAsync([.. cache], 0, cache.Count);
                        viewModel?.PrgSet(prg);
                        viewModel?.SetCurrentDownloadState(prg, fileSize - offset);
                        //viewModel?.ThreadsInfo = $"{(prg + offset) / (double)fileSize * 100}% {sw.GetTime()}";
                    }
                }
            }
            catch (Exception ex)
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

    /// <summary>
    /// Calculates the size.
    /// </summary>
    /// <param name="serverUri">The server URI.</param>
    /// <param name="promptOnError">if set to <c>true</c> [prompt on error].</param>
    /// <param name="unit">The unit.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <returns></returns>
    public static async Task<double> CalculateSizeAsync(Uri serverUri,
                                                        bool promptOnError = true,
                                                        LengthType unit = LengthType.Megabytes,
                                                        string username = "",
                                                        string password = "")
    {
        double bytesTotal = 0;

        try
        {
            if (serverUri.Scheme == Uri.UriSchemeHttp
                || serverUri.Scheme == Uri.UriSchemeHttps)
            {
                using var wc = new FlakWebClient();
#pragma warning disable IDISP004 // false positive, stream unused - only reading headers
                wc.OpenRead(serverUri);
#pragma warning restore IDISP004
                bytesTotal = Convert.ToInt64(wc.ResponseHeaders["Content-Length"]);
            }
            else if (serverUri.Scheme == Uri.UriSchemeFtp)
            {
                var request = (FtpWebRequest)serverUri.GetWebRequest();
                request.Proxy = null;
                request.Credentials = NetworkUtilities.GetCredentials(username, password);
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

    private static async Task<KeyValuePair<bool, FtpWebResponse>> TryGetResponseAsync(
        Uri serverUri,
        string username,
        string password,
        long offset)
    {
        // Get the object used to communicate with the server.
        var request = (FtpWebRequest)serverUri.GetWebRequest();
        request.Method = WebRequestMethods.Ftp.DownloadFile;
        request.Credentials = NetworkUtilities.GetCredentials(username, password);
        request.ContentOffset = offset;
        FtpWebResponse response = null;

        try
        {
#pragma warning disable IDISP001 // false positive, ownership transferred to caller via return value
            response = (FtpWebResponse)await request.GetResponseAsync();
#pragma warning restore IDISP001

            return new(true, response);
        }
        catch (Exception ex)
        {
            ex.HandleException();
        }

        return new(false, response);
    }

    private static async Task<long> DetectOffsetAsync(Uri serverUri,
                                                      long offset,
                                                      string username,
                                                      string password,
                                                      IProgressAggregator viewModel)
    {
        var newOffset = offset;

        if (serverUri.Scheme == Uri.UriSchemeFtp)
        {
            var fileSize = (long)await CalculateSizeAsync(serverUri, false, LengthType.Bytes, username, password);
            viewModel?.PrgSetMax(fileSize - offset);
            var readCount = 0;

            while (readCount <= 0
                   && newOffset < fileSize)
            {
                var resp = await TryGetResponseAsync(serverUri, username, password, offset);
                var response = resp.Value;

                if (!resp.Key)
                    return offset;

                try
                {
                    using var stream = response.GetResponseStream();

                    if (stream is not null)
                    {
                        var buffer = new byte[1];
                        readCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                        newOffset--;

                        while (readCount > 0)
                        {
                            var innerResp = await TryGetResponseAsync(serverUri, username, password, offset);

                            if (!innerResp.Key)
                                return newOffset;

                            using var innerStream = innerResp.Value.GetResponseStream();

                            if (innerStream is not null)
                            {
                                buffer = new byte[1];
                                readCount = await innerStream.ReadAsync(buffer, 0, buffer.Length);
                                viewModel?.PrgSet(newOffset - offset);
                                viewModel?.SetCurrentDownloadState(newOffset - offset, fileSize - offset);
                                newOffset--;
                            }
                        }
                    }
                }
                catch (Exception ex)
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