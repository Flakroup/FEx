#pragma warning disable IDISP001 // clients are released through the factory under test
using FluentFTP;
using FluentFTP.Proxy.AsyncProxy;
using Shouldly;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FEx.FTPx.Tests;

public sealed class FtpClientFactoryTests : IDisposable
{
    private static readonly TimeSpan Short = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan Long = TimeSpan.FromSeconds(10);
    private readonly FakeFtpServer _server = new();

    public void Dispose() => _server.Dispose();

    private async Task<FtpClientFactory> Factory(int maxParallel) =>
        await FtpClientFactory.GetInstanceAsync(_server.Uri.AbsoluteUri, maxParallel);

    private const string ProxyHost = "127.0.0.1";
    private static readonly NetworkCredential ProxyCredentials = new("proxy-user", "proxy-pass");

    /// <summary>
    ///     The proxy profile is not readable from the client, so it is observed on the wire: the client must reach the proxy
    ///     host on the proxy port, ask it for the FTP host and port, and send the proxy credentials.
    /// </summary>
    private async Task AssertConnectsThrough(AsyncFtpClient client, FakeProxyServer proxy)
    {
        await Should.ThrowAsync<Exception>(() => client.Connect(TestContext.Current.CancellationToken));

        var request = await proxy.FirstRequest.WaitAsync(Long, TestContext.Current.CancellationToken);

        request.ShouldContain($"CONNECT {_server.Uri.Host}:{_server.Port} HTTP/1.1");
        request.ShouldContain($"Proxy-Authorization: Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes("proxy-user:proxy-pass"))}");
    }

    private Uri UriWithPortAndPath() => new UriBuilder(_server.Uri) { Port = _server.Port, Path = "/some/path" }.Uri;

#pragma warning disable VSTHRD003 // the task is started by the caller on purpose, to observe whether it completes
    private static async Task<bool> Completes(Task task, TimeSpan within) =>
        await Task.WhenAny(task, Task.Delay(within, TestContext.Current.CancellationToken)) == task;
#pragma warning restore VSTHRD003

    [Fact]
    public async Task GetInstanceAsync_SameHost_ReturnsTheSameFactory()
    {
        var first = await Factory(2);
        var second = await Factory(9);

        second.ShouldBeSameAs(first);
        first.HostUri.ShouldBe(_server.Uri);
    }

    [Fact]
    public async Task GetInstanceAsync_DifferentHosts_ReturnDifferentFactories()
    {
        using var other = new FakeFtpServer();

        var first = await Factory(2);
        var second = await FtpClientFactory.GetInstanceAsync(other.Uri.AbsoluteUri, 2);

        second.ShouldNotBeSameAs(first);
    }

    [Fact]
    public async Task CreateAsync_UserAndPassword_ConfiguresHostCredentialsAndPort()
    {
        var factory = await Factory(2);

        var client = await factory.CreateAsync("alice", "secret", false, 2121);

        client.ShouldNotBeOfType<AsyncFtpClientHttp11Proxy>();
        client.Credentials.UserName.ShouldBe("alice");
        client.Credentials.Password.ShouldBe("secret");
        client.Port.ShouldBe(2121);
        await factory.ReleaseClientAsync(client);
    }

    [Fact]
    public async Task CreateAsync_HostUriWithPortAndPath_SetsTheBareHostAndTheUriPort()
    {
        var factory = await FtpClientFactory.GetInstanceAsync(UriWithPortAndPath().AbsoluteUri, 2);

        var client = await factory.CreateAsync("u", "p", false);

        client.Host.ShouldBe(_server.Uri.Host);
        client.Port.ShouldBe(_server.Port);
        await client.Connect(TestContext.Current.CancellationToken);
        client.IsConnected.ShouldBeTrue();
        await factory.ReleaseClientAsync(client);
    }

    [Fact]
    public async Task CreateAsync_HostUriWithPort_ExplicitPortArgumentWins()
    {
        var factory = await FtpClientFactory.GetInstanceAsync(UriWithPortAndPath().AbsoluteUri, 2);

        var client = await factory.CreateAsync("u", "p", false, 2121);

        client.Port.ShouldBe(2121);
        await factory.ReleaseClientAsync(client);
    }

    [Fact]
    public async Task CreateAsync_HostUriWithDefaultPort_KeepsTheFtpDefaultPort()
    {
        var factory = await FtpClientFactory.GetInstanceAsync($"ftp://{_server.Uri.Host}:21/dir", 2);

        var client = await factory.CreateAsync(null, null, false);

        client.Host.ShouldBe(_server.Uri.Host);
        client.Port.ShouldBe(21);
        await factory.ReleaseClientAsync(client);
    }

    [Fact]
    public async Task CreateAsync_Ipv6HostUri_SetsTheAddressWithoutBrackets()
    {
        var factory = await FtpClientFactory.GetInstanceAsync("ftp://[::1]:2121/some/path", 2);

        var client = await factory.CreateAsync(null, null, false);

        client.Host.ShouldBe("::1");
        client.Port.ShouldBe(2121);
        await factory.ReleaseClientAsync(client);
    }

    [Fact]
    public async Task ReleaseAsync_ClientNoFactoryCreated_IsDisposedWithoutTouchingAnyPool()
    {
        var factory = await Factory(1);
        var held = await factory.CreateAsync(null, null, false);
        var foreign = new AsyncFtpClient("127.0.0.1");

        await FtpCommon.ReleaseAsync(foreign);

        foreign.IsDisposed.ShouldBeTrue();
        (await Completes(factory.CreateAsync(null, null, false), Short)).ShouldBeFalse("a foreign release must not free the slot of an unrelated client");
        await factory.ReleaseClientAsync(held);
    }

    [Fact]
    public async Task CreateAsync_NoCredentialsAndNoPort_LeavesTheClientDefaults()
    {
        var factory = await Factory(2);

        var client = await factory.CreateAsync(null, null, false);

        client.Credentials.UserName.ShouldBe("anonymous");
        await factory.ReleaseClientAsync(client);
    }

    [Fact]
    public async Task CreateAsync_OnlyAPassword_StillCreatesCredentials()
    {
        var factory = await Factory(2);

        var client = await factory.CreateAsync(null, "pw", false);

        client.Credentials.Password.ShouldBe("pw");
        await factory.ReleaseClientAsync(client);
    }

    [Fact]
    public async Task CreateAsync_NetworkCredential_AreAppliedToTheClient()
    {
        var factory = await Factory(2);

        var client = await factory.CreateAsync(new NetworkCredential("bob", "pw"), false);

        client.Credentials.UserName.ShouldBe("bob");
        client.Credentials.Password.ShouldBe("pw");
        await factory.ReleaseClientAsync(client);
    }

    [Fact]
    public async Task CreateAsync_UseProxyWithProxyHost_ConnectsThroughTheFactoryProxySettings()
    {
        using var proxy = new FakeProxyServer();
        var factory = await Factory(2);
        factory.ProxyHost = ProxyHost;
        factory.ProxyPort = proxy.Port;
        factory.ProxyCredentials = ProxyCredentials;

        var client = await factory.CreateAsync(new NetworkCredential("bob", "pw"), true, _server.Port);

        client.ShouldBeOfType<AsyncFtpClientHttp11Proxy>();
        await AssertConnectsThrough(client, proxy);
        await factory.ReleaseClientAsync(client);
    }

    [Fact]
    public async Task CreateAsync_ProxyProfile_ConnectsThroughTheProfile()
    {
        using var proxy = new FakeProxyServer();
        var factory = await Factory(2);
        var profile = new FtpProxyProfile { ProxyHost = ProxyHost, ProxyPort = proxy.Port, ProxyCredentials = ProxyCredentials };

        var client = await factory.CreateAsync("bob", "pw", profile, _server.Port);

        client.ShouldBeOfType<AsyncFtpClientHttp11Proxy>();
        client.Credentials.UserName.ShouldBe("bob");
        await AssertConnectsThrough(client, proxy);
        await factory.ReleaseClientAsync(client);
    }

    [Fact]
    public async Task CreateAsync_NetworkCredentialAndProxyProfile_ConnectsThroughTheProfile()
    {
        using var proxy = new FakeProxyServer();
        var factory = await Factory(2);
        var profile = new FtpProxyProfile { ProxyHost = ProxyHost, ProxyPort = proxy.Port, ProxyCredentials = ProxyCredentials };

        var client = await factory.CreateAsync(new NetworkCredential("bob", "pw"), profile, _server.Port);

        client.ShouldBeOfType<AsyncFtpClientHttp11Proxy>();
        await AssertConnectsThrough(client, proxy);
        await factory.ReleaseClientAsync(client);
    }

    [Fact]
    public async Task GetProxy_WithoutProxy_IsNull() => (await Factory(2)).GetProxy(false).ShouldBeNull();

    [Fact]
    public async Task GetProxy_WithProxy_CopiesHostPortAndCredentialsToTheProfile()
    {
        var factory = await Factory(2);
        var creds = new NetworkCredential("p", "q");
        factory.ProxyHost = "proxy.local";
        factory.ProxyPort = 3128;
        factory.ProxyCredentials = creds;

        var profile = factory.GetProxy(true);

        profile.ShouldNotBeNull();
        profile.ProxyHost.ShouldBe("proxy.local");
        profile.ProxyPort.ShouldBe(3128);
        profile.ProxyCredentials.ShouldBeSameAs(creds);
    }

    [Fact]
    public async Task CreateAsync_AtMaxParallel_WaitsForARelease()
    {
        var factory = await Factory(2);
        var first = await factory.CreateAsync(null, null, false);
        var second = await factory.CreateAsync(null, null, false);

        var third = factory.CreateAsync(null, null, false);

        (await Completes(third, Short)).ShouldBeFalse("the third client must wait while two are out");

        await factory.ReleaseClientAsync(first);

        (await Completes(third, Long)).ShouldBeTrue("a release must let the waiting create through");
        await factory.ReleaseClientAsync(second);
        await factory.ReleaseClientAsync(await third);
    }

    [Fact]
    public async Task ReleaseClientAsync_FreesTheSlot_SoItIsReusedBySequentialCreates()
    {
        var factory = await Factory(1);

        for (var i = 0; i < 3; i++)
        {
            var next = factory.CreateAsync(null, null, false);
            (await Completes(next, Long)).ShouldBeTrue($"create #{i} must reuse the released slot");
            await factory.ReleaseClientAsync(await next);
        }
    }

    [Fact]
    public async Task ReleaseClientAsync_DisposesTheClient()
    {
        var factory = await Factory(1);
        var client = await factory.CreateAsync(null, null, false);

        await factory.ReleaseClientAsync(client);

        client.IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public async Task ReleaseClientAsync_ConnectedClient_SaysQuitAndDisposes()
    {
        var factory = await Factory(1);
        var client = await factory.CreateAsync("u", "p", false, _server.Port);
        await client.Connect(TestContext.Current.CancellationToken);
        client.IsConnected.ShouldBeTrue();

        await factory.ReleaseClientAsync(client);

        client.IsDisposed.ShouldBeTrue();
        _server.Commands.ShouldContain("QUIT");
    }

    [Fact]
    public async Task ReleaseClientAsync_AlreadyDisposedClient_StillFreesTheSlot()
    {
        var factory = await Factory(1);
        var client = await factory.CreateAsync(null, null, false);
#pragma warning disable IDISP016 // releasing an already disposed client is the case under test
        await client.DisposeAsync();
        await factory.ReleaseClientAsync(client);
#pragma warning restore IDISP016

        var next = factory.CreateAsync(null, null, false);
        (await Completes(next, Long)).ShouldBeTrue();
        await factory.ReleaseClientAsync(await next);
    }

    [Fact]
    public async Task ConnectedClients_NeverExceedMaxParallelAgainstTheServer()
    {
        var factory = await Factory(2);

        await Task.WhenAll(Enumerable.Range(0, 6).Select(async _ =>
        {
            var client = await factory.CreateAsync("u", "p", false, _server.Port);
            await client.Connect(TestContext.Current.CancellationToken);
            await Task.Delay(50, TestContext.Current.CancellationToken);
            await factory.ReleaseClientAsync(client);
        }));

        _server.MaxConcurrentConnections.ShouldBeLessThanOrEqualTo(2);
        _server.MaxConcurrentConnections.ShouldBeGreaterThan(0);
    }
}
