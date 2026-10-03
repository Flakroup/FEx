using Shouldly;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace FEx.FTPx.Tests;

/// <summary>Runs the real transport against a closed local port: no server, no external network.</summary>
public sealed class FtpTransportTests : IDisposable
{
    private static readonly Uri Unreachable = new("ftp://127.0.0.1:1/file.bin");
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public async Task GetSize_ServerUnreachable_Throws() =>
        await Should.ThrowAsync<WebException>(() =>
            FtpTransport.Instance.GetSizeAsync(Unreachable, "user", "pass", TestContext.Current.CancellationToken));

    [Fact]
    public async Task Open_ServerUnreachable_Throws() =>
        await Should.ThrowAsync<WebException>(async () =>
        {
            using var response = await FtpTransport.Instance.OpenAsync(Unreachable, "user", "pass", 0, TestContext.Current.CancellationToken);
        });

    [Fact]
    public async Task DownloadFile_PartialFileAndServerUnreachable_IsNotReportedComplete()
    {
        var path = Path.Combine(_dir, "partial.bin");
        await File.WriteAllBytesAsync(path, [1, 2, 3], TestContext.Current.CancellationToken);

        var ex = await Should.ThrowAsync<IOException>(() => Download(path, "user", "pass"));

        ex.InnerException.ShouldBeOfType<WebException>();
    }

    [Fact]
    public async Task DownloadFile_DefaultCredentials_FailsOnTheNetworkNotOnTheCredentials()
    {
        var ex = await Should.ThrowAsync<IOException>(() => Download(Path.Combine(_dir, "new.bin"), "", ""));

        ex.InnerException.ShouldBeOfType<WebException>();
    }

    [Fact]
    public void IsLocalProcessingAbort_NonFtpFailures_AreNotAborts()
    {
        FtpTransport.Instance.IsLocalProcessingAbort(new InvalidOperationException()).ShouldBeFalse();
        FtpTransport.Instance.IsLocalProcessingAbort(new WebException("no response")).ShouldBeFalse();
    }

    private static Task<bool> Download(string path, string username, string password) =>
        FtpDownloader.DownloadFileAsync(FtpTransport.Instance,
            TimeSpan.Zero,
            path,
            Unreachable,
            null,
            username,
            password,
            2,
            TestContext.Current.CancellationToken);
}
