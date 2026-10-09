#pragma warning disable IDISP001, IDISP007 // sockets are owned by the serving loop and closed in its finally block
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.FTPx.Tests;

/// <summary>
///     A minimal in-process FTP server on the loopback interface: login, passive-mode listing, download and upload of
///     in-memory files. It exists so the FluentFTP calls run for real without any network.
/// </summary>
internal sealed class FakeFtpServer : IDisposable
{
    private static int _nextAddress;
    private readonly IPAddress _address = NextAddress();
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentQueue<string> _commands = new();
    private int _open;
    private int _maxOpen;

    public FakeFtpServer()
    {
        _listener = new(_address, 0);
        _listener.Start();
        _ = Task.Run(AcceptLoop);
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>Remote files by absolute path (<c>/dir/name.ext</c>).</summary>
    public ConcurrentDictionary<string, byte[]> Files { get; } = new();

    public string? Password { get; set; }

    public IReadOnlyCollection<string> Commands => [.. _commands];

    public int MaxConcurrentConnections => Volatile.Read(ref _maxOpen);

    public int OpenConnections => Volatile.Read(ref _open);

    /// <summary>Host only, without the port: <c>FtpClient.Host</c> keeps whatever follows the scheme, so the port is passed separately.</summary>
    public Uri Uri => new($"ftp://{_address}/");

    /// <summary>Every server listens on its own loopback address, so each gets its own <see cref="FtpClientFactory" /> (keyed by host).</summary>
    private static IPAddress NextAddress()
    {
        var n = Interlocked.Increment(ref _nextAddress);

        return IPAddress.Parse($"127.{10 + (n >> 8)}.{n & 255}.1");
    }

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
        var now = Interlocked.Increment(ref _open);
        int seen;

        while (now > (seen = Volatile.Read(ref _maxOpen))
               && Interlocked.CompareExchange(ref _maxOpen, now, seen) != seen)
        {
        }

        TcpListener? data = null;

        try
        {
            using var _ = client;
            var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\r\n", AutoFlush = true };
            var user = "";
            await writer.WriteLineAsync("220 fake ftp");

            while (await reader.ReadLineAsync() is { } line)
            {
                _commands.Enqueue(line);
                var space = line.IndexOf(' ');
                var cmd = (space < 0 ? line : line[..space]).ToUpperInvariant();
                var arg = space < 0 ? "" : line[(space + 1)..];

                switch (cmd)
                {
                    case "USER":
                        user = arg;
                        await writer.WriteLineAsync("331 password please");
                        break;
                    case "PASS":
                        await writer.WriteLineAsync(Password == null || arg == Password ? $"230 welcome {user}" : "530 login incorrect");
                        break;
                    case "FEAT":
                        await writer.WriteLineAsync("211-Features:\r\n SIZE\r\n MDTM\r\n211 End");
                        break;
                    case "PWD":
                        await writer.WriteLineAsync("257 \"/\" is the current directory");
                        break;
                    case "TYPE" or "CWD" or "NOOP" or "OPTS":
                        await writer.WriteLineAsync("200 ok");
                        break;
                    case "SIZE":
                        await writer.WriteLineAsync(Files.TryGetValue(arg, out var sized) ? $"213 {sized.Length}" : "550 not found");
                        break;
                    case "MDTM":
                        await writer.WriteLineAsync(Files.ContainsKey(arg) ? "213 20260101120000" : "550 not found");
                        break;
                    case "DELE":
                        await writer.WriteLineAsync(Files.TryRemove(arg, out var removed) ? "250 deleted" : "550 not found");
                        break;
                    case "PASV":
                        data?.Stop();
                        data = new(_address, 0);
                        data.Start();
                        var p = ((IPEndPoint)data.LocalEndpoint).Port;
                        await writer.WriteLineAsync($"227 Entering Passive Mode ({_address.ToString().Replace('.', ',')},{p / 256},{p % 256})");
                        break;
                    case "LIST" or "NLST":
                        await Transfer(writer, data, async d => await d.WriteAsync(Listing(arg)));
                        break;
                    case "RETR":
                        if (Files.TryGetValue(arg, out var content))
                            await Transfer(writer, data, async d => await d.WriteAsync(content));
                        else
                            await writer.WriteLineAsync("550 not found");

                        break;
                    case "STOR":
                        await Transfer(writer, data, async d =>
                        {
                            using var ms = new MemoryStream();
                            await d.CopyToAsync(ms);
                            Files[arg] = ms.ToArray();
                        }, true);

                        break;
                    case "QUIT":
                        await writer.WriteLineAsync("221 bye");

                        return;
                    default:
                        await writer.WriteLineAsync("502 not implemented");
                        break;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            // client went away
        }
        finally
        {
            data?.Stop();
            Interlocked.Decrement(ref _open);
        }
    }

    private static async Task Transfer(StreamWriter control, TcpListener? data, Func<Stream, Task> body, bool upload = false)
    {
        if (data == null)
        {
            await control.WriteLineAsync("425 no data connection");

            return;
        }

        await control.WriteLineAsync("150 opening data connection");
        using var conn = await data.AcceptTcpClientAsync();
        var stream = conn.GetStream();
        await body(stream);

        if (!upload)
            await stream.FlushAsync();

        conn.Close();
        await control.WriteLineAsync("226 transfer complete");
    }

    private byte[] Listing(string dir)
    {
        var prefix = dir.TrimEnd('/') + "/";
        var sb = new StringBuilder();

        foreach (var f in Files.Where(x => x.Key.StartsWith(prefix, StringComparison.Ordinal) && !x.Key[prefix.Length..].Contains('/')))
            sb.Append($"-rw-r--r-- 1 owner group {f.Value.Length} Jan 01 2026 {f.Key[prefix.Length..]}\r\n");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
