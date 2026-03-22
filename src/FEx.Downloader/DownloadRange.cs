using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Agnostics.Abstractions.Models;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Agnostics.BaseObjects;
using FEx.Core.Abstractions.Extensions;
using FEx.Downloader.Abstractions.Interfaces;
using FEx.Downloader.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Downloader;

public class DownloadRange : NotifyPropertyChanged, IDownloadRange, IDisposable
{
    private long _size;
    private bool _isConnected;

    public static int BufferSize { get; } =
        Convert.ToInt32(FileLengthConverter.ConvertFileLength(80, LengthType.Kilobytes, LengthType.Bytes, 0));

    public long MaxChunkSize { get; }
    public long ExpectedSize { get; }
    public long From { get; private set; }
    public long To { get; private set; }
    public string FilePath { get; }
    public IProgress<bool> ConnPrg { get; }
    public long DataLength { get; }
    public DirectoryInfo Dir { get; }
    public DownloadState DState { get; set; }
    public WebRequestParams Pars { get; }
    public Uri Url { get; }
    public Dictionary<int, DownloadChunk> Chunks { get; }
    public string RangeHeader { get; private set; }
    public ContentRangeHeaderValue RangeHeaderValue { get; private set; }
    public string DirPath => Dir.FullName;

    public long Size
    {
        get => _size;
        protected set => SetProperty(ref _size, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        protected set => SetProperty(ref _isConnected, value, x => ConnPrg?.Report(x));
    }

    protected byte[] Buffer { get; }

    protected long ReadenBytes { get; set; }
    private CancellationToken CancellationToken { get; }

    public DownloadRange(long from,
                         long to,
                         DirectoryInfo directory,
                         Uri url,
                         WebRequestParams pars,
                         long maxChunkSize,
                         string filePath,
                         long dataLength)
        : this(from, to, directory, url, pars, maxChunkSize, filePath, dataLength, null, null, default)
    {
    }

    public DownloadRange(long from,
                         long to,
                         DirectoryInfo directory,
                         Uri url,
                         WebRequestParams pars,
                         long maxChunkSize,
                         string filePath,
                         long dataLength,
                         IProgress<double> progress,
                         IProgress<bool> connPrg,
                         CancellationToken token)
    {
        From = from;
        To = to;

        ExpectedSize = To - From + 1;

        Buffer = new byte[BufferSize];
        Dir = directory;

        Pars = pars;

        Url = url;
        CancellationToken = token;
        MaxChunkSize = maxChunkSize;
        FilePath = filePath;
        ConnPrg = connPrg;
        DataLength = dataLength;
        Chunks = [];

        var length = To - From + 1;
        double operatingSize = Math.Min(length, MaxChunkSize);
        var chunksCount = (int)Math.Ceiling(length / operatingSize);
        var offset = From;

        foreach (var chunkNr in Enumerable.Range(0, chunksCount))
        {
            var end = (long)Math.Min(offset + operatingSize - 1, To);
            Chunks.Add(chunkNr, new(offset, end, Dir, progress));
            offset = end + 1;
        }

        HandleChunks();
    }

    public async Task DoDownloadAsync(int retryCount)
    {
        var from = From;
        var to = To;

        while (retryCount >= 0
               && DState != DownloadState.Finished)
        {
            var unfinishedChunks = Chunks.Where(x => x.Value.State != DownloadState.Finished)
                .OrderBy(x => x.Value.From)
                .Select(x => x.Key)
                .ToArray();

            if (unfinishedChunks.Length > 0)
            {
                var ranges = GetRanges(unfinishedChunks);

                foreach (var range in ranges)
                    await ProcessDownloadAsync(range.start, range.end);
            }

            retryCount--;
        }

        From = from;
        To = to;
    }

    public void SetContentRange(string rangeHeader)
    {
        RangeHeader = rangeHeader;
        RangeHeaderValue = RangeHeader.GetContentRange();
        From = RangeHeaderValue?.From ?? -1;
        To = RangeHeaderValue?.To ?? -1;
    }

    private static (int start, int end)[] GetRanges(int[] unfinishedChunks)
    {
        var res = new List<int[]>();

        foreach (var t in unfinishedChunks)
        {
            if (res.Count == 0
                || res[res.Count - 1][1] != t - 1)
                res.Add([t, t]);
            else
                res[res.Count - 1][1] = t;
        }

        return res.Select(x => (start: x[0], end: x[1])).ToArray();
    }

    private void HandleChunks()
    {
        var isFinished = true;
        long s = 0;

        foreach (var c in Chunks.Values)
        {
            s += c.Size;

            if (c.State != DownloadState.Finished)
                isFinished = false;
        }

        Size = s;

        if (DState != DownloadState.Failed
            && DState != DownloadState.Cancelled)
            DState = isFinished
                ? DownloadState.Finished
                : DownloadState.InProgress;
    }

    private async Task ProcessDownloadAsync(int startChunkKey, int endChunkKey)
    {
        try
        {
            From = Chunks[startChunkKey].From;
            To = Chunks[endChunkKey].To;
            ReadenBytes = 0;

            DState = DownloadState.None;
            var myHttpWebRequest = Url.GetHttpRequest(Pars);
            myHttpWebRequest.AddRange(From, To);

            DState = DownloadState.Connecting;
            using var res = await myHttpWebRequest.GetResponseAsync();
            using var response = (HttpWebResponse)res;
            var retrievedContentRange = response.GetContentRange();

            if (retrievedContentRange?.From is null
                || retrievedContentRange.To is null
                || retrievedContentRange.From.Value != From
                || retrievedContentRange.To.Value != To
                || response.ContentLength != To - From + 1)
            {
                DState = DownloadState.Failed;
            }
            else
            {
                using var streamResponse = response.GetResponseStream();

                if (streamResponse is null)
                    DState = DownloadState.Failed;
                else
                    try
                    {
                        DState = DownloadState.InProgress;
                        OpenedConnection();

                        var isReading = true;
                        int bytesRead;
                        int receivedBytes;

                        while (isReading)
                        {
                            bytesRead = 0;
                            receivedBytes = -1;
                            Array.Clear(Buffer, 0, Buffer.Length);

                            while (bytesRead < Buffer.Length
                                   && receivedBytes != 0
                                   && !CancellationToken.IsCancellationRequested)
                            {
                                receivedBytes = await streamResponse.ReadAsync(Buffer,
                                    bytesRead,
                                    Buffer.Length - bytesRead,
                                    CancellationToken);

                                bytesRead += receivedBytes;
                            }

                            isReading = bytesRead > 0;

                            if (isReading)
                                await DumpBufferToChunksAsync(bytesRead);
                        }
                    }
                    finally
                    {
                        ClosedConnection();
                    }
            }
        }
        catch (Exception ex)
        {
            DState = CancellationToken.IsCancellationRequested
                ? DownloadState.Cancelled
                : DownloadState.Failed;

            if (DState == DownloadState.Failed)
            {
                ClearChunks();
                ex.HandleException(false);

                await WaitForInternetConnectionAsync();
            }
        }
        finally
        {
            if (DState != DownloadState.Failed
                && DState != DownloadState.Cancelled)
                HandleChunks();
        }
    }

    private async Task WaitForInternetConnectionAsync()
    {
        bool isAvailable;

        do
        {
            isAvailable = await Url.CheckForInternetConnectionAsync();

            if (!isAvailable)
                await Task.Delay(1000, CancellationToken);
        } while (!isAvailable);
    }

    private void ClearChunks()
    {
        foreach (var c in Chunks.Values)
            c.ClearChunk();
    }

    private async Task DumpBufferToChunksAsync(int bytesRead)
    {
        var offset = 0;

        while (bytesRead > 0)
        {
            var start = From + ReadenBytes;
            var chunkNo = Chunks.First(x => x.Value.From <= start && x.Value.To >= start).Key;
            var gapSize = (int)(Chunks[chunkNo].ExpectedSize - Chunks[chunkNo].Size);
            var bytesToWrite = Math.Min(bytesRead, gapSize);

            await Chunks[chunkNo].WriteBytesAsync(Buffer, offset, bytesToWrite, CancellationToken);
            ReadenBytes += bytesToWrite;
            Size += bytesToWrite;
            offset += bytesToWrite;
            bytesRead -= bytesToWrite;
        }
    }

    private void ClosedConnection() => IsConnected = false;

    private void OpenedConnection() => IsConnected = true;

    #region IComparable
    public override bool Equals(object obj) => Equals(obj as IDownloadBase);

    public bool Equals(IDownloadBase other) => other is not null && FilePath == other.FilePath && Url == other.Url;

    public override int GetHashCode() =>
        BitConverter.ToInt32(Encoding.UTF8.GetBytes($"{FilePath}@{Url.AbsoluteUri}"), 0);

    public void Dispose() => Chunks?.Values.ForEachInEnumerable(x => x?.Dispose());

    public int CompareTo(object obj) =>
        Equals(obj)
            ? 0
            : GetHashCode().CompareTo(obj.GetHashCode());

    public int CompareTo(IDownloadBase other) =>
        Equals(other)
            ? 0
            : GetHashCode().CompareTo(other.GetHashCode());
    #endregion
}