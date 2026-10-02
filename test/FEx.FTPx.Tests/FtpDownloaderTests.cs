#pragma warning disable IDISP004 // fake responses are tracked and their disposal is asserted
using FEx.Core.Abstractions.Interfaces;
using FEx.MVVM;
using FEx.MVVM.Abstractions.Interfaces;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
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
    public async Task DownloadFile_NoResponse_StopsAtCapAndReturnsFalse()
    {
        var transport = new FakeTransport { OpenReturnsNull = true };

        (await Download(transport, NewPath(), 3)).ShouldBeFalse();
        transport.OpenCalls.ShouldBe(3);
    }

    [Fact]
    public async Task DownloadFile_Cancelled_StopsRetrying()
    {
        var transport = new FakeTransport { OpenReturnsNull = true };
        using var cts = new CancellationTokenSource();
        transport.OnOpen = () => cts.Cancel();

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

        (await FtpDownloader.RestartDownloadFromServerAsync(transport, new(), NewPath(), Server, null, 0, "", ""))
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
        var stateA = new DownloadState();
        var stateB = new DownloadState();
        var pathA = NewPath();
        var pathB = NewPath();

        for (var i = 0; i < DownloadState.MaxReadRetries; i++)
        {
            transport.Responses.Enqueue(new FakeResponse(new ScriptedStream(null, new AbortException())));
            await FtpDownloader.RestartDownloadFromServerAsync(transport, stateA, pathA, Server, null, 0, "", "");

            transport.Responses.Enqueue(new FakeResponse(new ScriptedStream([1])));
            await FtpDownloader.RestartDownloadFromServerAsync(transport, stateB, pathB, Server, null, 0, "", "");
            File.Delete(pathB);
        }

        stateA.RetryCount.ShouldBe(DownloadState.MaxReadRetries);
        transport.Responses.Enqueue(new FakeResponse(new ScriptedStream(null, new AbortException())));
        transport.Responses.Enqueue(new FakeResponse(new ScriptedStream([])));
        var before = transport.OpenCalls;

        await FtpDownloader.RestartDownloadFromServerAsync(transport, stateA, pathA, Server, null, 0, "", "");

        // The 11th failure triggers DetectOffsetAsync, which opens more responses.
        transport.OpenCalls.ShouldBeGreaterThan(before + 1);
        stateA.RetryCount.ShouldBe(0);
        transport.Created.ShouldAllBe(r => r.Disposed);
    }

    private sealed class AbortException : Exception;

    private sealed class FakeTransport : IFtpTransport
    {
        public long Size { get; set; }
        public Exception? OpenThrows { get; set; }
        public bool OpenReturnsNull { get; set; }
        public bool AbortExceptions { get; set; }
        public Action? OnOpen { get; set; }
        public int OpenCalls { get; private set; }
        public Queue<FakeResponse> Responses { get; } = new();
        public List<FakeResponse> Created { get; } = [];

        public Task<long> GetSizeAsync(Uri serverUri, string username, string password) => Task.FromResult(Size);

        public Task<IFtpResponse?> OpenAsync(Uri serverUri, string username, string password, long offset)
        {
            OpenCalls++;
            OnOpen?.Invoke();

            if (OpenThrows is not null)
                throw OpenThrows;

            if (OpenReturnsNull)
                return Task.FromResult<IFtpResponse?>(null);

            // Probes past the scripted responses get an empty (EOF) stream.
            var response = Responses.Count > 0 ? Responses.Dequeue() : new FakeResponse(new ScriptedStream([]));
            Created.Add(response);

            return Task.FromResult<IFtpResponse?>(response);
        }

        public bool IsLocalProcessingAbort(Exception exception) => AbortExceptions && exception is AbortException;
    }

    private sealed class FakeResponse(Stream stream) : IFtpResponse
    {
        public bool Disposed { get; private set; }
        public string StatusDescription => "226 ok";

        public Stream? GetResponseStream() => stream;

        public void Dispose() => Disposed = true;
    }

    /// <summary>Yields <paramref name="data" /> once, then EOF; or throws <paramref name="error" /> on first read.</summary>
    private sealed class ScriptedStream(byte[]? data, Exception? error = null) : Stream
    {
        private bool _done;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (error is not null)
                throw error;

            if (_done || data is null)
                return 0;

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
