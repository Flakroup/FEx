#pragma warning disable IDISP001 // clients are released through FtpCommon under test
using FEx.Logging;
using FEx.Logging.Abstractions.Interfaces;
using FEx.MVVM.Abstractions.Interfaces;
using FluentFTP;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FEx.FTPx.Tests;

/// <summary>Runs the real <see cref="FtpCommon" /> and FluentFTP against <see cref="FakeFtpServer" /> (loopback, no network).</summary>
public sealed class FtpCommonTests : IDisposable
{
    private static readonly byte[] Payload = Encoding.UTF8.GetBytes("hello over ftp");
    private readonly FakeFtpServer _server = new();
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;
    private readonly IFExLoggingService _log = Substitute.For<IFExLoggingService>();

    public FtpCommonTests() => _ = new FExLoggingModule(_log, Substitute.For<IFExLoggingConfigurator>());

    public void Dispose()
    {
        _server.Dispose();
        Directory.Delete(_dir, true);
    }

    private Uri Host => _server.Uri;

    private int Port => _server.Port;

    private async Task LimitToOneClient() => await FtpClientFactory.GetInstanceAsync(Host.AbsoluteUri, 1);

    private static async Task Eventually(Func<bool> condition)
    {
        var until = DateTime.UtcNow.AddSeconds(5);

        while (!condition() && DateTime.UtcNow < until)
            await Task.Delay(20, TestContext.Current.CancellationToken);

        condition().ShouldBeTrue();
    }

    [Fact]
    public async Task CreateAsync_ReturnsAnAsyncClientForTheHostCredentialsAndPort()
    {
        var client = await FtpCommon.CreateAsync(Host, "alice", "secret", false, Port);

        client.ShouldBeOfType<AsyncFtpClient>();
        client.Credentials.UserName.ShouldBe("alice");
        client.Credentials.Password.ShouldBe("secret");
        client.Port.ShouldBe(Port);
        await FtpCommon.ReleaseAsync(client);
    }

    [Fact]
    public async Task ReleaseAsync_DisposesTheClientAndFreesItsSlot()
    {
        await LimitToOneClient();
        var client = await FtpCommon.CreateAsync(Host, "u", "p", false, Port);

        await FtpCommon.ReleaseAsync(client);

        client.IsDisposed.ShouldBeTrue();
        var next = FtpCommon.CreateAsync(Host, "u", "p", false, Port);
        (await Task.WhenAny(next, Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken))).ShouldBe(next);
        await FtpCommon.ReleaseAsync(await next);
    }

    [Fact]
    public async Task GetFtpFileInfoAsync_ExistingFile_ReturnsItsListItem()
    {
        _server.Files["/pub/data.bin"] = Payload;

        var item = await FtpCommon.GetFtpFileInfoAsync("/pub/data.bin", Host, "u", "p", false, Port);

        item.ShouldNotBeNull();
        item.Size.ShouldBe(Payload.Length);
        item.Name.ShouldBe("data.bin");
    }

    [Fact]
    public async Task GetFtpFileInfoAsync_ServerRejectsTheLogin_ReturnsNullAndFreesTheSlot()
    {
        await LimitToOneClient();
        _server.Password = "right";

        var rejected = await FtpCommon.GetFtpFileInfoAsync("/x", Host, "u", "wrong", false, Port);

        rejected.ShouldBeNull();
        _log.Received(1).Log(typeof(FtpCommon), LogLevel.Information, Arg.Any<string>(), Arg.Any<Exception?>());
        _server.Files["/x"] = Payload;
        var item = await FtpCommon.GetFtpFileInfoAsync("/x", Host, "u", "right", false, Port).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        item.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetListingAsync_ReturnsTheFilesOfTheDirectory()
    {
        _server.Files["/pub/a.txt"] = [1, 2, 3];
        _server.Files["/pub/b.txt"] = [4];
        _server.Files["/other/c.txt"] = [5];

        var listing = await FtpCommon.GetListingAsync("/pub", Host, "u", "p", false, Port);

        listing.Select(x => x.Name).Order().ShouldBe(["a.txt", "b.txt"]);
    }

    [Fact]
    public async Task GetListingAsync_NullHost_ReturnsAnEmptyArrayWithoutConnecting()
    {
        var listing = await FtpCommon.GetListingAsync("/pub", null!, "u", "p", false, Port);

        listing.ShouldBeEmpty();
        _server.Commands.ShouldBeEmpty();
    }

    [Fact]
    public async Task DownloadFileFtpAsync_ExistingFile_WritesItLocallyAndReportsProgress()
    {
        _server.Files["/pub/data.bin"] = Payload;
        var progress = Substitute.For<IProgressAggregator>();

        var path = await FtpCommon.DownloadFileFtpAsync(_dir, Host, "/pub/data.bin", "u", "p", progress, false, Port);

        path.ShouldNotBeNull().ShouldBe(Path.Combine(_dir, "data.bin"));
        (await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken)).ShouldBe(Payload);
        progress.Received().PrgSetMax(Payload.Length);
        await Eventually(() => progress.ReceivedCalls().Any(c => c.GetMethodInfo().Name == nameof(IProgressAggregator.PrgSet)));
    }

    [Fact]
    public async Task DownloadFileFtpAsync_BracketsInTheName_ReturnsNullWithoutConnecting()
    {
        _server.Files["/pub/a[1].bin"] = Payload;

        var path = await FtpCommon.DownloadFileFtpAsync(_dir, Host, "/pub/a[1].bin", "u", "p", null, false, Port);

        path.ShouldBeNull();
        _server.Commands.ShouldBeEmpty();
    }

    [Fact]
    public async Task DownloadFileFtpAsync_ServerFails_ThrowsAndFreesTheSlot()
    {
        await LimitToOneClient();
        _server.Password = "right";

        await Should.ThrowAsync<Exception>(() => FtpCommon.DownloadFileFtpAsync(_dir, Host, "/pub/data.bin", "u", "wrong", null, false, Port));

        _server.Files["/pub/data.bin"] = Payload;
        _server.Password = null;
        var path = await FtpCommon.DownloadFileFtpAsync(_dir, Host, "/pub/data.bin", "u", "p", null, false, Port).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        path.ShouldNotBeNull();
    }

    [Fact]
    public async Task UploadFileFtpAsync_NewFile_UploadsItAndReportsSuccess()
    {
        var local = Path.Combine(_dir, "up.bin");
        await File.WriteAllBytesAsync(local, Payload, TestContext.Current.CancellationToken);
        var progress = Substitute.For<IProgressAggregator>();

        var status = await FtpCommon.UploadFileFtpAsync(local, Host, "/in", "u", "p", _ => throw new InvalidOperationException("not asked"), progress, false, Port);

        status.ShouldBe(FtpStatus.Success);
        _server.Files["/in/up.bin"].ShouldBe(Payload);
        progress.Received().PrgSetMax(Payload.Length);
    }

    [Fact]
    public async Task UploadFileFtpAsync_ExistingFileAndCallbackDeclines_LeavesTheRemoteFileAndReturnsNull()
    {
        var local = Path.Combine(_dir, "up.bin");
        await File.WriteAllBytesAsync(local, Payload, TestContext.Current.CancellationToken);
        _server.Files["/in/up.bin"] = [9];
        string? asked = null;

        var status = await FtpCommon.UploadFileFtpAsync(local, Host, "/in", "u", "p", t =>
        {
            asked = t;

            return false;
        }, null, false, Port);

        status.ShouldBeNull();
        asked.ShouldBe("/in/up.bin");
        _server.Files["/in/up.bin"].ShouldBe([9]);
    }

    [Fact]
    public async Task UploadFileFtpAsync_ExistingFileAndCallbackAccepts_OverwritesIt()
    {
        var local = Path.Combine(_dir, "up.bin");
        await File.WriteAllBytesAsync(local, Payload, TestContext.Current.CancellationToken);
        _server.Files["/in/up.bin"] = [9];

        var status = await FtpCommon.UploadFileFtpAsync(local, Host, "/in", "u", "p", _ => true, null, false, Port);

        status.ShouldBe(FtpStatus.Success);
        _server.Files["/in/up.bin"].ShouldBe(Payload);
    }

    [Fact]
    public async Task UploadFileFtpAsync_NoFileName_ReturnsNullWithoutConnecting()
    {
        var status = await FtpCommon.UploadFileFtpAsync(null!, Host, "/in", "u", "p", _ => true, null, false, Port);

        status.ShouldBeNull();
        _server.Commands.ShouldBeEmpty();
    }
}
