#pragma warning disable IDISP001, IDISP004 // fake responses/streams are tracked and their disposal is asserted
using FEx.Core.Abstractions.Interfaces;
using FEx.MVVM;
using FEx.MVVM.Abstractions.Interfaces;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.FTPx.Tests;

public sealed class FtpDownloaderTests : IDisposable
{
    private static readonly Uri Server = new("ftp://example.invalid/file.bin");
    private static readonly TimeSpan NoDelay = TimeSpan.Zero;
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    private string NewPath() => Path.Combine(_dir, Guid.NewGuid() + ".bin");

    private static Task<bool> Download(FakeTransport t,
                                       string path,
                                       int maxAttempts = 5,
                                       CancellationToken? ct = null) =>
        FtpDownloader.DownloadFileAsync(t, NoDelay, path, Server, null, "", "", maxAttempts, ct ?? TestContext.Current.CancellationToken);

    [Fact]
    public async Task DownloadFile_PermanentFailure_StopsAtCapAndSurfacesTheError()
    {
        var transport = new FakeTransport { OpenThrows = new InvalidOperationException("530 login") };

        var ex = await Should.ThrowAsync<IOException>(() => Download(transport, NewPath(), 3));

        ex.InnerException.ShouldBeSameAs(transport.OpenThrows);
        transport.OpenCalls.ShouldBe(3);
    }

    [Fact]
    public async Task DownloadFile_CapMessage_DoesNotLeakUriCredentials()
    {
        var transport = new FakeTransport { OpenThrows = new InvalidOperationException("530 login") };
        var uri = new Uri("ftp://alice:s3cret@example.invalid/dir/file.bin");

        var ex = await Should.ThrowAsync<IOException>(() =>
            FtpDownloader.DownloadFileAsync(transport, NoDelay, NewPath(), uri, null, "", "", 1, TestContext.Current.CancellationToken));

        ex.Message.ShouldNotContain("s3cret");
        ex.Message.ShouldContain("example.invalid/dir/file.bin");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DownloadFile_InvalidMaxAttempts_Throws(int maxAttempts)
    {
        var transport = new FakeTransport();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => Download(transport, NewPath(), maxAttempts));

        transport.OpenCalls.ShouldBe(0);
    }

    [Fact]
    public async Task DownloadFile_ProgressResetsTheAttemptCap()
    {
        // Every attempt delivers one byte and then fails: 5 attempts are needed with a cap of 2.
        var transport = new FakeTransport { Size = 5 };

        for (var i = 0; i < 5; i++)
            transport.Responses.Enqueue(new FakeResponse(new ScriptedStream([1], new InvalidOperationException("drop"))));

        var path = NewPath();

        (await Download(transport, path, 2)).ShouldBeTrue();

        new FileInfo(path).Length.ShouldBe(5);
        transport.OpenCalls.ShouldBe(5);
    }

    [Fact]
    public async Task DownloadFile_CancelledDuringTransfer_StopsReadingAndThrows()
    {
        var transport = new FakeTransport { Size = 100 };
        using var cts = new CancellationTokenSource();
        var stream = new ScriptedStream([1]) { OnRead = cts.Cancel };
        transport.Responses.Enqueue(new FakeResponse(stream));

        await Should.ThrowAsync<OperationCanceledException>(() => Download(transport, NewPath(), 5, cts.Token));

        stream.Reads.ShouldBe(1);
        transport.Created.ShouldAllBe(r => r.Disposed);
    }

    [Fact]
    public async Task DownloadFile_Cancelled_StopsRetrying()
    {
        var transport = new FakeTransport { OpenThrows = new InvalidOperationException("down") };
        using var cts = new CancellationTokenSource();
        transport.OnOpen = cts.Cancel;

        await Should.ThrowAsync<OperationCanceledException>(() => Download(transport, NewPath(), 1000, cts.Token));

        transport.OpenCalls.ShouldBe(1);
    }

    [Fact]
    public async Task DownloadFile_Success_ReturnsTrueAndDisposesResponse()
    {
        var transport = new FakeTransport { Size = 4 };
        transport.Responses.Enqueue(new FakeResponse(new ScriptedStream([1, 2, 3, 4])));
        var path = NewPath();

        (await Download(transport, path)).ShouldBeTrue();

        (await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken)).ShouldBe(new byte[] { 1, 2, 3, 4 });
        transport.Created.ShouldAllBe(r => r.Disposed);
    }

    [Fact]
    public async Task Restart_ReadFailure_DisposesResponse()
    {
        var transport = new FakeTransport { Size = 4 };
        transport.Responses.Enqueue(new FakeResponse(new ScriptedStream(null, new InvalidOperationException("boom"))));

        (await FtpDownloader.RestartDownloadFromServerAsync(transport, new(), NewPath(), Server, null, 0, "", "", TestContext.Current.CancellationToken))
            .ShouldBeFalse();

        transport.Created.Count.ShouldBe(1);
        transport.Created.ShouldAllBe(r => r.Disposed);
    }

    [Fact]
    public async Task Restart_RetryCountIsNotSharedBetweenDownloads()
    {
        // Download A aborts 10 times; download B reads successfully in between every time.
        // With one shared counter B's successful read resets A's, so A would never reach the threshold.
        _ = new FExMvvm(Substitute.For<IMessagePopupService>(), Substitute.For<IExceptionHandler>());
        var transport = new FakeTransport { Size = 1_000_000, AbortExceptions = true };
        var stateA = new FtpDownloadState();
        var stateB = new FtpDownloadState();
        var pathA = NewPath();
        var pathB = NewPath();

        for (var i = 0; i < FtpDownloadState.MaxReadRetries; i++)
        {
            transport.Responses.Enqueue(new FakeResponse(new ScriptedStream(null, new AbortException())));
            await FtpDownloader.RestartDownloadFromServerAsync(transport, stateA, pathA, Server, null, 0, "", "", TestContext.Current.CancellationToken);

            transport.Responses.Enqueue(new FakeResponse(new ScriptedStream([1])));
            await FtpDownloader.RestartDownloadFromServerAsync(transport, stateB, pathB, Server, null, 0, "", "", TestContext.Current.CancellationToken);
            File.Delete(pathB);
        }

        stateA.RetryCount.ShouldBe(FtpDownloadState.MaxReadRetries);
        transport.Responses.Enqueue(new FakeResponse(new ScriptedStream(null, new AbortException())));
        transport.Responses.Enqueue(new FakeResponse(new ScriptedStream([])));
        var before = transport.OpenCalls;

        await FtpDownloader.RestartDownloadFromServerAsync(transport, stateA, pathA, Server, null, 0, "", "", TestContext.Current.CancellationToken);

        // The 11th failure triggers DetectOffsetAsync, which opens more responses.
        transport.OpenCalls.ShouldBeGreaterThan(before + 1);
        stateA.RetryCount.ShouldBe(0);
        new FileInfo(pathA).Length.ShouldBeLessThanOrEqualTo(transport.Size);
        transport.Created.ShouldAllBe(r => r.Disposed);
    }

    [Fact]
    public async Task Restart_ZeroFill_NeverGrowsTheFilePastTheRemoteSize()
    {
        _ = new FExMvvm(Substitute.For<IMessagePopupService>(), Substitute.For<IExceptionHandler>());
        var transport = new FakeTransport { Size = 1_000_000, AbortExceptions = true };
        var state = new FtpDownloadState { RetryCount = FtpDownloadState.MaxReadRetries };
        var path = NewPath();
        transport.Responses.Enqueue(new FakeResponse(new ScriptedStream(null, new AbortException())));

        await FtpDownloader.RestartDownloadFromServerAsync(transport, state, path, Server, null, 0, "", "", TestContext.Current.CancellationToken);

        new FileInfo(path).Length.ShouldBe(1_000_000);
    }

    [Fact]
    public async Task Restart_OffsetProbe_DisposesEveryProbeResponse()
    {
        _ = new FExMvvm(Substitute.For<IMessagePopupService>(), Substitute.For<IExceptionHandler>());
        var transport = new FakeTransport { Size = 1_000_000, AbortExceptions = true };
        var state = new FtpDownloadState { RetryCount = FtpDownloadState.MaxReadRetries };
        transport.Responses.Enqueue(new FakeResponse(new ScriptedStream(null, new AbortException())));
        transport.Responses.Enqueue(new FakeResponse(new ScriptedStream([])));  // probe at the start: nothing served
        transport.Responses.Enqueue(new FakeResponse(new ScriptedStream([1]))); // probe one chunk further: data, so the chunk is bisected

        await FtpDownloader.RestartDownloadFromServerAsync(transport, state, NewPath(), Server, null, 0, "", "", TestContext.Current.CancellationToken);

        transport.Created.Count.ShouldBeGreaterThanOrEqualTo(4);
        transport.Created.ShouldAllBe(r => r.Disposed);
    }

    [Fact]
    public async Task Restart_ParallelRecoveries_OnASmallPool_BothFinish()
    {
        _ = new FExMvvm(Substitute.For<IMessagePopupService>(), Substitute.For<IExceptionHandler>());
        var transport = new CappedTransport(3);
        var ct = TestContext.Current.CancellationToken;

        var a = FtpDownloader.RestartDownloadFromServerAsync(transport, new FtpDownloadState { RetryCount = FtpDownloadState.MaxReadRetries }, NewPath(), new("ftp://example.invalid/a.bin"), null, 0, "", "", ct);
        var b = FtpDownloader.RestartDownloadFromServerAsync(transport, new FtpDownloadState { RetryCount = FtpDownloadState.MaxReadRetries }, NewPath(), new("ftp://example.invalid/b.bin"), null, 0, "", "", ct);

        await Task.WhenAll(a, b).WaitAsync(TimeSpan.FromSeconds(10), ct);

        // One download holds at most one connection while it recovers: the dead transfer is released before the probe opens.
        transport.Peak.ShouldBeLessThanOrEqualTo(2);
        transport.Live.ShouldBe(0);
    }

    [Fact]
    public async Task Restart_Recovery_ProbesTheWalkedOffset_AndZeroFillsExactlyToTheFirstReadableByte()
    {
        _ = new FExMvvm(Substitute.For<IMessagePopupService>(), Substitute.For<IExceptionHandler>());
        var transport = new FakeTransport { Size = 2_000_000, AbortExceptions = true, ReadableFrom = 700_000 };
        var path = NewPath();
        transport.Responses.Enqueue(new FakeResponse(new ScriptedStream(null, new AbortException())));

        await FtpDownloader.RestartDownloadFromServerAsync(transport, new FtpDownloadState { RetryCount = FtpDownloadState.MaxReadRetries }, path, Server, null, 0, "", "", TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // The dying transfer, then the probes at the start and one chunk (512 KiB) further, which find nothing, then the chunk
        // after that, which serves data; the chunk before it is bisected for the first readable byte.
        transport.Offsets.Take(4).ShouldBe([0, 0, 524_288, 1_048_576]);
        new FileInfo(path).Length.ShouldBe(700_000);
        transport.OpenCalls.ShouldBeLessThanOrEqualTo(4 + 19);
        transport.Created.ShouldAllBe(r => r.Disposed);
    }

    [Fact]
    public async Task Restart_Recovery_WhenTheServerServesAtTheStart_ProbesOnceAndFillsNothing()
    {
        _ = new FExMvvm(Substitute.For<IMessagePopupService>(), Substitute.For<IExceptionHandler>());
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var transport = new FakeTransport { Size = 1_000_000, AbortExceptions = true, ReadableFrom = 0 };

        // A probe loop that never ends opens connections without bound: stop it, and fail on the count.
        transport.OnOpen = () =>
        {
            if (transport.OpenCalls > 100)
                cts.Cancel();

            cts.Token.ThrowIfCancellationRequested();
        };

        var path = NewPath();
        transport.Responses.Enqueue(new FakeResponse(new ScriptedStream(null, new AbortException())));

        await FtpDownloader.RestartDownloadFromServerAsync(transport, new FtpDownloadState { RetryCount = FtpDownloadState.MaxReadRetries }, path, Server, null, 0, "", "", cts.Token);

        transport.Offsets.ShouldBe([0, 0]);
        new FileInfo(path).Length.ShouldBe(0);
    }

    private sealed class AbortException : Exception;

    /// <summary>
    ///     Models the per-host client pool: every open response holds one of <paramref name="cap" /> slots until it is
    ///     disposed. The first open per path is a transfer that dies with an abort once both downloads have one; later
    ///     ones are the offset probe.
    /// </summary>
    private sealed class CappedTransport(int cap) : IFtpTransport
    {
        private readonly SemaphoreSlim _slots = new(cap);
        private readonly ConcurrentDictionary<string, int> _opens = new();
        private readonly object _gate = new();
        private readonly TaskCompletionSource _bothTransfersOpen = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _transfers;

        public int Peak { get; private set; }
        public int Live { get; private set; }

        public async Task<long> GetSizeAsync(Uri serverUri, string username, string password, CancellationToken cancellationToken)
        {
            await _slots.WaitAsync(cancellationToken);
            _slots.Release();

            return 1_000_000;
        }

        public async Task<IFtpResponse> OpenAsync(Uri serverUri, string username, string password, long offset, CancellationToken cancellationToken)
        {
            await _slots.WaitAsync(cancellationToken);

            lock (_gate)
            {
                Live++;
                Peak = Math.Max(Peak, Live);
            }

            var n = _opens.AddOrUpdate(serverUri.AbsolutePath, 1, (_, v) => v + 1);

            // Both downloads hold their transfer before either fails, so their recoveries really overlap.
            if (n == 1)
            {
                if (Interlocked.Increment(ref _transfers) == 2)
                    _bothTransfersOpen.TrySetResult();

                await _bothTransfersOpen.Task.WaitAsync(cancellationToken);
            }

            Stream stream = n == 1 ? new ScriptedStream(null, new AbortException()) : new ScriptedStream(n == 2 ? [1] : []);

            return new SlotResponse(stream, () =>
            {
                lock (_gate)
                    Live--;

                _slots.Release();
            });
        }

        public bool IsLocalProcessingAbort(Exception exception) => exception is AbortException;
    }

    private sealed class SlotResponse(Stream stream, Action release) : IFtpResponse
    {
        private int _disposed;

        public string StatusDescription => "226 ok";

        public Stream? GetResponseStream() => stream;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                release();

            return default;
        }
    }

    private sealed class FakeTransport : IFtpTransport
    {
        public long Size { get; set; }
        public Exception? OpenThrows { get; set; }
        public bool AbortExceptions { get; set; }
        public Action? OnOpen { get; set; }
        public int OpenCalls { get; private set; }

        /// <summary>The first offset the server serves a byte at; a probe below it reads nothing. Never, unless set.</summary>
        public long ReadableFrom { get; set; } = long.MaxValue;

        public List<long> Offsets { get; } = [];
        public Queue<FakeResponse> Responses { get; } = new();
        public List<FakeResponse> Created { get; } = [];

        public Task<long> GetSizeAsync(Uri serverUri, string username, string password, CancellationToken cancellationToken) => Task.FromResult(Size);

        public Task<IFtpResponse> OpenAsync(Uri serverUri, string username, string password, long offset, CancellationToken cancellationToken)
        {
            OpenCalls++;
            Offsets.Add(offset);
            OnOpen?.Invoke();

            if (OpenThrows is not null)
                throw OpenThrows;

            // Probes past the scripted responses get an empty (EOF) stream.
            var response = Responses.Count > 0
                ? Responses.Dequeue()
                : new(new ScriptedStream(offset >= ReadableFrom ? [1] : []));
            Created.Add(response);

            return Task.FromResult<IFtpResponse>(response);
        }

        public bool IsLocalProcessingAbort(Exception exception) => AbortExceptions && exception is AbortException;
    }

    private sealed class FakeResponse(Stream stream) : IFtpResponse
    {
        public bool Disposed { get; private set; }
        public string StatusDescription => "226 ok";

        public Stream? GetResponseStream() => stream;

        public ValueTask DisposeAsync()
        {
            Disposed = true;

            return default;
        }
    }

    /// <summary>Yields <paramref name="data" /> once, then EOF, or throws <paramref name="error" /> when no data is left.</summary>
    private sealed class ScriptedStream(byte[]? data, Exception? error = null) : Stream
    {
        private bool _done;

        public Action? OnRead { get; init; }
        public int Reads { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Reads++;
            OnRead?.Invoke();

            if (_done || data is null)
                return error is null ? 0 : throw error;

            _done = true;
            data.CopyTo(buffer, offset);

            return data.Length;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.FromResult(Read(buffer, offset, count));

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
