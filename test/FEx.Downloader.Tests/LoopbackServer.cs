using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FEx.Downloader.Tests;

/// <summary>A real HTTP server on the loopback interface, for behaviour that only exists on the real handler.</summary>
internal sealed class LoopbackServer : IDisposable
{
    private readonly HttpListener _listener = new();

    public Uri Url { get; }

    public LoopbackServer(Action<HttpListenerContext> handle, AuthenticationSchemes schemes = AuthenticationSchemes.Anonymous)
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        Url = new($"http://127.0.0.1:{port}/");
        _listener.Prefixes.Add(Url.AbsoluteUri);
        _listener.AuthenticationSchemes = schemes;
        _listener.Start();

        _ = Task.Run(async () =>
        {
            while (_listener.IsListening)
                try
                {
                    var context = await _listener.GetContextAsync();
                    handle(context);
                    context.Response.Close();
                }
                catch (Exception) when (!_listener.IsListening)
                {
                    return;
                }
                catch (Exception)
                {
                    // a failing request must not stop the server
                }
        });
    }

    public void Dispose() => _listener.Close();
}
