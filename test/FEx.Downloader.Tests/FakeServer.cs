using FEx.Downloader;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Downloader.Tests;

/// <summary>A fake file server honouring <c>Range</c>, with a hook to override single answers.</summary>
internal sealed class FakeServer
{
    private int _inFlight;
    private int _peakInFlight;

    public byte[] Data { get; }
    public bool AcceptRanges { get; init; } = true;

    /// <summary>Called for every request; a non-null result replaces the normal answer.</summary>
    public Func<HttpRequestMessage, HttpResponseMessage?>? Override { get; init; }

    public TimeSpan Latency { get; init; } = TimeSpan.Zero;
    public FakeHttpHandler Handler { get; }
    public int PeakInFlight => _peakInFlight;
    public int RequestCount => Handler.Requests.Count;

    public FakeServer(byte[] data)
    {
        Data = data;
        Handler = new(HandleAsync);
    }

    public HttpClient CreateClient() => new(Handler);

    /// <summary>True for the ranged requests the item sends while downloading in parallel (not the 0..BufferLength probes).</summary>
    public static bool IsRangeChunk(HttpRequestMessage request) =>
        request.Headers.Range?.Ranges.Single() is { } range
        && (range.From != 0 || range.To != DownloadItem.BufferLength);

    private async Task<HttpResponseMessage> HandleAsync(HttpRequestMessage request, CancellationToken token)
    {
        var inFlight = Interlocked.Increment(ref _inFlight);

        try
        {
            int peak;

            while (inFlight > (peak = _peakInFlight)
                   && Interlocked.CompareExchange(ref _peakInFlight, inFlight, peak) != peak)
            {
            }

            if (Latency > TimeSpan.Zero)
                await Task.Delay(Latency, token);

            return Override?.Invoke(request) ?? Serve(request);
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }

    private HttpResponseMessage Serve(HttpRequestMessage request)
    {
        var range = request.Headers.Range?.Ranges.Single();

        if (range is null
            || !AcceptRanges)
        {
            var full = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Data)
            };

            if (AcceptRanges)
                full.Headers.AcceptRanges.Add("bytes");

            return full;
        }

        var from = (int)range.From!.Value;
        var to = (int)Math.Min(range.To!.Value, Data.Length - 1);
        var content = new ByteArrayContent(Data[from..(to + 1)]);
        content.Headers.ContentRange = new(from, to, Data.Length);

        return new(HttpStatusCode.PartialContent)
        {
            Content = content
        };
    }
}
