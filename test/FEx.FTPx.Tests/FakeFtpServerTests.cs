#pragma warning disable IDISP001, IDISP004 // the raw sessions are disposed with the test
using Shouldly;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.FTPx.Tests;

/// <summary>Pins the session rules of <see cref="FakeFtpServer" /> that the transport tests lean on.</summary>
public sealed class FakeFtpServerTests : IDisposable
{
    private static readonly byte[] Payload = [10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
    private readonly FakeFtpServer _server = new();

    public FakeFtpServerTests() => _server.Files["/pub/data.bin"] = Payload;

    public void Dispose() => _server.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Rest_FollowedByARetrOfAMissingFile_DoesNotShiftTheNextRetr()
    {
        using var session = await Session.OpenAsync(_server);

        await session.SendAsync("REST 4");
        (await session.SendAsync("RETR /pub/missing.bin")).ShouldStartWith("550");

        (await session.RetrAsync("/pub/data.bin")).ShouldBe(Payload);
    }

    [Fact]
    public async Task Rest_OnOneSession_DoesNotShiftTheRetrOfAnother()
    {
        using var a = await Session.OpenAsync(_server);
        using var b = await Session.OpenAsync(_server);

        await a.SendAsync("REST 4");

        (await b.RetrAsync("/pub/data.bin")).ShouldBe(Payload);
        (await a.RetrAsync("/pub/data.bin")).ShouldBe(Payload[4..]);
    }

    [Fact]
    public async Task Rest_IsUsedByOneTransferOnly()
    {
        using var session = await Session.OpenAsync(_server);

        await session.SendAsync("REST 4");
        (await session.RetrAsync("/pub/data.bin")).ShouldBe(Payload[4..]);

        (await session.RetrAsync("/pub/data.bin")).ShouldBe(Payload);
    }

    /// <summary>A bare control connection: enough FTP to log in, go passive and download.</summary>
    private sealed class Session : IDisposable
    {
        private readonly TcpClient _control;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        private Session(TcpClient control)
        {
            _control = control;
            var stream = control.GetStream();
            _reader = new(stream, Encoding.UTF8);
            _writer = new(stream, new UTF8Encoding(false)) { NewLine = "\r\n", AutoFlush = true };
        }

        public static async Task<Session> OpenAsync(FakeFtpServer server)
        {
            var control = new TcpClient();
            await control.ConnectAsync(IPAddress.Parse(server.Uri.Host), server.Port, Ct);
            var session = new Session(control);
            (await session._reader.ReadLineAsync(Ct)).ShouldNotBeNull().ShouldStartWith("220");

            return session;
        }

        public void Dispose()
        {
            _reader.Dispose();
            _writer.Dispose();
            _control.Dispose();
        }

        public async Task<string> SendAsync(string command)
        {
            await _writer.WriteLineAsync(command.AsMemory(), Ct);

            return await _reader.ReadLineAsync(Ct) ?? throw new IOException("The server closed the control connection.");
        }

        public async Task<byte[]> RetrAsync(string path)
        {
            var pasv = await SendAsync("PASV");
            var numbers = pasv[(pasv.IndexOf('(') + 1)..pasv.IndexOf(')')].Split(',');
            var port = (int.Parse(numbers[4]) << 8) + int.Parse(numbers[5]);

            using var data = new TcpClient();
            await data.ConnectAsync(IPAddress.Parse(string.Join('.', numbers[..4])), port, Ct);

            (await SendAsync("RETR " + path)).ShouldStartWith("150");

            using var received = new MemoryStream();
            await data.GetStream().CopyToAsync(received, Ct);
            (await _reader.ReadLineAsync(Ct)).ShouldNotBeNull().ShouldStartWith("226");

            return received.ToArray();
        }
    }
}
