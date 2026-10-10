#pragma warning disable IDISP001, IDISP004 // responses are disposed through await using; the pool tests dispose them on purpose
using FEx.Agnostics.Abstractions.Enums;
using FluentFTP.Exceptions;
using Shouldly;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.FTPx.Tests;

/// <summary>Runs the real transport against <see cref="FakeFtpServer" /> (loopback) and against a closed local port.</summary>
public sealed class FtpTransportTests : IDisposable
{
    private static readonly byte[] Payload = [10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
    private static readonly Uri Unreachable = new("ftp://127.0.0.1:1/file.bin");
    private readonly FakeFtpServer _server = new();
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    public FtpTransportTests() => _server.Files["/pub/data.bin"] = Payload;

    public void Dispose()
    {
        _server.Dispose();
        Directory.Delete(_dir, true);
    }

    private Uri ServerFile => new UriBuilder(_server.Uri) { Port = _server.Port, Path = "/pub/data.bin" }.Uri;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task LimitToOneClient() =>
        await FtpClientFactory.GetInstanceAsync(new UriBuilder(_server.Uri) { Port = _server.Port }.Uri.AbsoluteUri, 1);

    private static async Task<byte[]> ReadAll(IFtpResponse response)
    {
        using var ms = new MemoryStream();
        var stream = response.GetResponseStream().ShouldNotBeNull();
        var buffer = new byte[4];
        int read;

        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, Ct)) > 0)
            await ms.WriteAsync(buffer, 0, read, Ct);

        return ms.ToArray();
    }

    [Fact]
    public async Task GetSize_ExistingFile_ReturnsTheRemoteSize() =>
        (await FtpTransport.Instance.GetSizeAsync(ServerFile, "u", "p", Ct)).ShouldBe(Payload.Length);

    [Fact]
    public async Task GetSize_MissingFile_ThrowsInsteadOfReportingZero()
    {
        var missing = new UriBuilder(ServerFile) { Path = "/pub/none.bin" }.Uri;

        await Should.ThrowAsync<FtpException>(() => FtpTransport.Instance.GetSizeAsync(missing, "u", "p", Ct));
    }

    [Fact]
    public async Task GetSize_ServerUnreachable_Throws() =>
        await Should.ThrowAsync<Exception>(() => FtpTransport.Instance.GetSizeAsync(Unreachable, "user", "pass", Ct));

    [Fact]
    public async Task GetSize_ReleasesTheConnectionEvenWhenItFails()
    {
        await LimitToOneClient();
        _server.Password = "right";

        await Should.ThrowAsync<Exception>(() => FtpTransport.Instance.GetSizeAsync(ServerFile, "u", "wrong", Ct));

        _server.Password = null;
        (await FtpTransport.Instance.GetSizeAsync(ServerFile, "u", "p", Ct).WaitAsync(TimeSpan.FromSeconds(10), Ct)).ShouldBe(Payload.Length);
    }

    [Fact]
    public async Task GetSize_ExplicitCredentials_AreUsedToLogIn()
    {
        _server.Password = "right";

        await Should.ThrowAsync<Exception>(() => FtpTransport.Instance.GetSizeAsync(ServerFile, "u", "wrong", Ct));
        (await FtpTransport.Instance.GetSizeAsync(ServerFile, "u", "right", Ct)).ShouldBe(Payload.Length);
    }

    [Fact]
    public async Task GetSize_BlankCredentials_FallBackToTheUriUserInfo()
    {
        _server.Password = "s3 cret";
        var withUserInfo = new UriBuilder(ServerFile) { UserName = "alice", Password = "s3%20cret" }.Uri;

        (await FtpTransport.Instance.GetSizeAsync(withUserInfo, "", "", Ct)).ShouldBe(Payload.Length);
        _server.Commands.ShouldContain("USER alice");
    }

    [Fact]
    public async Task Open_AtZero_StreamsTheWholeFileWithoutRest()
    {
        await using var response = await FtpTransport.Instance.OpenAsync(ServerFile, "u", "p", 0, Ct);

        (await ReadAll(response)).ShouldBe(Payload);
        _server.Commands.ShouldNotContain(c => c.StartsWith("REST", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Open_AtOffset_ResumesWithRestAndStreamsTheRest()
    {
        await using var response = await FtpTransport.Instance.OpenAsync(ServerFile, "u", "p", 4, Ct);

        (await ReadAll(response)).ShouldBe(Payload[4..]);
        _server.Commands.ShouldContain("REST 4");
    }

    [Fact]
    public async Task Open_StatusDescription_IsTheReplyThatOpenedTheTransfer()
    {
        await using var response = await FtpTransport.Instance.OpenAsync(ServerFile, "u", "p", 0, Ct);

        response.StatusDescription.ShouldStartWith("150 ");
    }

    [Fact]
    public async Task Open_MissingFile_ThrowsAndReleasesTheConnection()
    {
        await LimitToOneClient();
        var missing = new UriBuilder(ServerFile) { Path = "/pub/none.bin" }.Uri;

        await Should.ThrowAsync<FtpException>(() => FtpTransport.Instance.OpenAsync(missing, "u", "p", 0, Ct));

        (await FtpTransport.Instance.GetSizeAsync(ServerFile, "u", "p", Ct).WaitAsync(TimeSpan.FromSeconds(10), Ct)).ShouldBe(Payload.Length);
    }

    [Fact]
    public async Task Open_DisposingTheResponse_ReleasesTheConnectionAndSaysQuit()
    {
        await LimitToOneClient();
        var response = await FtpTransport.Instance.OpenAsync(ServerFile, "u", "p", 0, Ct);
        var next = FtpTransport.Instance.GetSizeAsync(ServerFile, "u", "p", Ct);

        (await Task.WhenAny(next, Task.Delay(TimeSpan.FromMilliseconds(300), Ct))).ShouldNotBe(next, "the open response holds the only connection");

        await response.DisposeAsync();

        (await next.WaitAsync(TimeSpan.FromSeconds(10), Ct)).ShouldBe(Payload.Length);
        _server.Commands.ShouldContain("QUIT");
    }

    [Fact]
    public async Task Open_DisposingTheResponseTwice_ReleasesItsSlotOnlyOnce()
    {
        await LimitToOneClient();
        var response = await FtpTransport.Instance.OpenAsync(ServerFile, "u", "p", 0, Ct);

#pragma warning disable IDISP016 // disposing twice is the behaviour under test
        await response.DisposeAsync();
        await response.DisposeAsync();
#pragma warning restore IDISP016

        await using var first = await FtpTransport.Instance.OpenAsync(ServerFile, "u", "p", 0, Ct);
        var second = FtpTransport.Instance.OpenAsync(ServerFile, "u", "p", 0, Ct);

        (await Task.WhenAny(second, Task.Delay(TimeSpan.FromMilliseconds(300), Ct))).ShouldNotBe(second, "the second dispose must not hand out a second slot");

        await first.DisposeAsync();
        await using var released = await second.WaitAsync(TimeSpan.FromSeconds(10), Ct);
    }

    [Fact]
    public async Task Open_ServerAbortsTheTransfer_TheReadThatHitsTheEndFailsWithA451()
    {
        _server.AbortRetrAfterBytes = 3;
        await using var response = await FtpTransport.Instance.OpenAsync(ServerFile, "u", "p", 0, Ct);

        var ex = await Should.ThrowAsync<FtpCommandException>(() => ReadAll(response));

        ex.CompletionCode.ShouldBe("451");
        FtpTransport.Instance.IsLocalProcessingAbort(ex).ShouldBeTrue();
    }

    [Fact]
    public void IsLocalProcessingAbort_OnlyAFtp451IsAnAbort()
    {
        FtpTransport.Instance.IsLocalProcessingAbort(new FtpCommandException("451", "local error")).ShouldBeTrue();
        FtpTransport.Instance.IsLocalProcessingAbort(new FtpCommandException("550", "no such file")).ShouldBeFalse();
        FtpTransport.Instance.IsLocalProcessingAbort(new FtpException("451")).ShouldBeFalse();
        FtpTransport.Instance.IsLocalProcessingAbort(new InvalidOperationException()).ShouldBeFalse();
        FtpTransport.Instance.IsLocalProcessingAbort(new IOException()).ShouldBeFalse();
    }

    [Theory]
    [InlineData("ftp://host/dir/file.bin", "dir/file.bin")]
    [InlineData("ftp://host/file.bin", "file.bin")]
    [InlineData("ftp://host//abs/file.bin", "/abs/file.bin")]
    [InlineData("ftp://host/%2Fabs/file.bin", "/abs/file.bin")]
    [InlineData("ftp://host:2121/my%20dir/a%5B1%5D.bin", "my dir/a[1].bin")]
    public void RemotePath_IsRelativeToTheLoginDirectoryUnlessSpelledAbsolute(string uri, string expected) =>
        FtpTransport.RemotePath(new(uri)).ShouldBe(expected);

    [Fact]
    public void GetCredentials_BothPartsGiven_WinOverTheUriUserInfo()
    {
        var credentials = FtpTransport.GetCredentials(new("ftp://alice:x@host/f"), "bob", "pw").ShouldNotBeNull();

        credentials.UserName.ShouldBe("bob");
        credentials.Password.ShouldBe("pw");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("bob", "")]
    [InlineData("", "pw")]
    [InlineData(" ", " ")]
    public void GetCredentials_AnEmptyPart_KeepsTheUriUserInfo(string user, string password)
    {
        var credentials = FtpTransport.GetCredentials(new("ftp://al%40ice:p%3Aw@host/f"), user, password).ShouldNotBeNull();

        credentials.UserName.ShouldBe("al@ice");
        credentials.Password.ShouldBe("p:w");
    }

    [Fact]
    public void GetCredentials_UserInfoWithoutPassword_HasAnEmptyPassword() =>
        FtpTransport.GetCredentials(new("ftp://alice@host/f"), "", "").ShouldNotBeNull().Password.ShouldBe(string.Empty);

    [Fact]
    public void GetCredentials_NothingGiven_IsAnonymous() =>
        FtpTransport.GetCredentials(new("ftp://host/f"), "", "").ShouldBeNull();

    [Fact]
    public async Task DownloadFile_PartialFile_ResumesFromItsLength()
    {
        var path = Path.Combine(_dir, "partial.bin");
        await File.WriteAllBytesAsync(path, Payload[..4], Ct);

        (await Download(path, ServerFile, "u", "p")).ShouldBeTrue();

        (await File.ReadAllBytesAsync(path, Ct)).ShouldBe(Payload);
        _server.Commands.ShouldContain("REST 4");
    }

    [Fact]
    public async Task DownloadFile_NewFile_DownloadsItWhole()
    {
        var path = Path.Combine(_dir, "new.bin");

        (await Download(path, ServerFile, "", "")).ShouldBeTrue();

        (await File.ReadAllBytesAsync(path, Ct)).ShouldBe(Payload);
    }

    [Fact]
    public async Task DownloadFile_PartialFileAndServerUnreachable_IsNotReportedComplete()
    {
        var path = Path.Combine(_dir, "unreachable.bin");
        await File.WriteAllBytesAsync(path, [1, 2, 3], Ct);

        var ex = await Should.ThrowAsync<IOException>(() => Download(path, Unreachable, "user", "pass"));

        ex.InnerException.ShouldNotBeNull();
        new FileInfo(path).Length.ShouldBe(3);
    }

    [Fact]
    public async Task CalculateSize_FtpFile_ReturnsTheRemoteSizeInTheRequestedUnit()
    {
        (await FtpDownloader.CalculateSizeAsync(ServerFile, false, LengthType.Bytes, "u", "p", cancellationToken: Ct)).ShouldBe(Payload.Length);
    }

    [Fact]
    public async Task CalculateSize_MissingFtpFile_ReportsZeroInsteadOfThrowing()
    {
        var missing = new UriBuilder(ServerFile) { Path = "/pub/none.bin" }.Uri;

        (await FtpDownloader.CalculateSizeAsync(missing, false, LengthType.Bytes, "u", "p", cancellationToken: Ct)).ShouldBe(0);
    }

    private static Task<bool> Download(string path, Uri uri, string username, string password) =>
        FtpDownloader.DownloadFileAsync(FtpTransport.Instance, TimeSpan.Zero, path, uri, null, username, password, 2, Ct);
}
