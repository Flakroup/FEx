using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Webx.Utilities;
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

/// <summary>An open FTP download response; disposing it releases the underlying connection.</summary>
internal interface IFtpResponse : IDisposable
{
    string StatusDescription { get; }

    Stream? GetResponseStream();
}

internal sealed class FtpTransport : IFtpTransport
{
    public static FtpTransport Instance { get; } = new();

    public async Task<long> GetSizeAsync(Uri serverUri,
                                         string username,
                                         string password,
                                         CancellationToken cancellationToken)
    {
        var request = (FtpWebRequest)serverUri.GetWebRequest();
        request.Proxy = null;
        request.ApplyCredentials(username, password);
        request.Method = WebRequestMethods.Ftp.GetFileSize;

        using var registration = cancellationToken.Register(request.Abort);
        using var response = (FtpWebResponse)await request.GetResponseAsync();

        return response.ContentLength;
    }

    public async Task<IFtpResponse> OpenAsync(Uri serverUri,
                                              string username,
                                              string password,
                                              long offset,
                                              CancellationToken cancellationToken)
    {
        var request = (FtpWebRequest)serverUri.GetWebRequest();
        request.Method = WebRequestMethods.Ftp.DownloadFile;
        request.ApplyCredentials(username, password);
        request.ContentOffset = offset;

        using var registration = cancellationToken.Register(request.Abort);

        return new Response((FtpWebResponse)await request.GetResponseAsync());
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

internal static class FtpWebRequestExtensions
{
    /// <summary>
    /// Sets explicit credentials when both parts are given; otherwise keeps the request default (anonymous, or the
    /// URI user info), because <see cref="FtpWebRequest" /> rejects <see cref="CredentialCache.DefaultNetworkCredentials" />.
    /// </summary>
    public static void ApplyCredentials(this FtpWebRequest request, string username, string password)
    {
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            request.Credentials = NetworkUtilities.GetCredentials(username, password);
    }
}
