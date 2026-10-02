using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Core.Abstractions.Extensions;
using FEx.Webx.Utilities;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace FEx.FTPx;

/// <summary>Per-download retry bookkeeping; never shared between downloads.</summary>
internal sealed class DownloadState
{
    public const int MaxReadRetries = 10;

    public int RetryCount { get; set; }
}

/// <summary>Seam over the FTP wire calls so <see cref="FtpDownloader" /> can be tested without a server.</summary>
internal interface IFtpTransport
{
    Task<long> GetSizeAsync(Uri serverUri, string username, string password);

    /// <summary>Opens the file at <paramref name="offset" />; <c>null</c> when the server did not answer.</summary>
    Task<IFtpResponse?> OpenAsync(Uri serverUri, string username, string password, long offset);

    bool IsLocalProcessingAbort(Exception exception);
}

/// <summary>An open FTP download response; disposing it releases the underlying connection.</summary>
internal interface IFtpResponse : IDisposable
{
    string StatusDescription { get; }

    Stream? GetResponseStream();
}

internal sealed class FtpTransport : IFtpTransport
{
    public static FtpTransport Instance { get; } = new();

    public async Task<long> GetSizeAsync(Uri serverUri, string username, string password) =>
        (long)await FtpDownloader.CalculateSizeAsync(serverUri, false, LengthType.Bytes, username, password);

    public async Task<IFtpResponse?> OpenAsync(Uri serverUri, string username, string password, long offset)
    {
        var request = (FtpWebRequest)serverUri.GetWebRequest();
        request.Method = WebRequestMethods.Ftp.DownloadFile;
        request.Credentials = NetworkUtilities.GetCredentials(username, password);
        request.ContentOffset = offset;

        try
        {
            return new Response((FtpWebResponse)await request.GetResponseAsync());
        }
        catch (Exception ex)
        {
            ex.HandleException();

            return null;
        }
    }

    public bool IsLocalProcessingAbort(Exception exception) =>
        exception is WebException { Response: FtpWebResponse { StatusCode: FtpStatusCode.ActionAbortedLocalProcessingError } };

    private sealed class Response(FtpWebResponse inner) : IFtpResponse
    {
        public string StatusDescription => inner.StatusDescription ?? string.Empty;

        public Stream? GetResponseStream() => inner.GetResponseStream();

#pragma warning disable IDISP007 // ownership of the response was transferred to this wrapper
        public void Dispose() => inner.Dispose();
#pragma warning restore IDISP007
    }
}
