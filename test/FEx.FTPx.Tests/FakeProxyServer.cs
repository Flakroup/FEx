#pragma warning disable IDISP001, IDISP007 // sockets are owned by the accept loop and closed in its finally block
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.FTPx.Tests;

/// <summary>
///     Stands in for an HTTP proxy on the loopback interface: records the header block of the first request it receives
///     (the <c>CONNECT</c> an FTP client sends) and refuses it, so a test can read what the client put on the wire.
/// </summary>
internal sealed class FakeProxyServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _cts = new();
    private readonly TaskCompletionSource<string> _firstRequest = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public FakeProxyServer()
    {
        _listener.Start();
        _ = Task.Run(AcceptLoop);
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>The header lines of the first request, one per line.</summary>
    public Task<string> FirstRequest => _firstRequest.Task;

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        _cts.Dispose();
    }

    private async Task AcceptLoop()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = Task.Run(() => Serve(client));
            }
        }
        catch (Exception ex) when (ex is ObjectDisposedException or SocketException or InvalidOperationException)
        {
            // listener stopped
        }
    }

    private async Task Serve(TcpClient client)
    {
        try
        {
            using var _ = client;
            var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\r\n", AutoFlush = true };
            var request = new StringBuilder();

            while (await reader.ReadLineAsync() is { Length: > 0 } line)
                request.AppendLine(line);

            _firstRequest.TrySetResult(request.ToString());
            await writer.WriteLineAsync("HTTP/1.1 403 Forbidden");
            await writer.WriteLineAsync();

            // The refused client still says QUIT when it is released; answering keeps that from waiting out a timeout.
            while (await reader.ReadLineAsync() is not null)
                await writer.WriteLineAsync("221 bye");
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            // client went away
        }
    }
}
