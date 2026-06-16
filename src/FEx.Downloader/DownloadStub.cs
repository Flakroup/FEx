using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Models;
using FEx.Agnostics.BaseObjects;
using FEx.Downloader.Abstractions.Interfaces;
using FEx.Downloader.Enums;
using FEx.Webx.Extensions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FEx.Downloader;

public class DownloadStub : NotifyPropertyChanged, IDownloadStub
{
    private long _dataLength;
    private string _filePath;
    private string _dirPath;

    public string MD5Checksum { get; }
    public Uri Url { get; }

    public WebRequestParams Pars { get; }
    public int ParallelRanges { get; }
    public DownloadState DState { get; private set; }

    public string FilePath
    {
        get => _filePath;
        set =>
            SetProperty(ref _filePath,
                value,
                fP => DirPath = fP is not null
                    ? Directory.GetParent(fP).FullName
                    : null);
    }

    public string DirPath
    {
        get => _dirPath;
        set =>
            SetProperty(ref _dirPath,
                value,
                dP =>
                {
                    if (FilePath is not null)
                        FilePath = dP is not null
                            ? Path.Combine(dP, Path.GetFileName(FilePath))
                            : null;
                });
    }

    public long DataLength
    {
        get => _dataLength;
        set => SetProperty(ref _dataLength, value);
    }

    public DownloadStub()
    {
    }

    public DownloadStub(string url, string filePath)
        : this(new Uri(url), filePath, null, null, 50)
    {
    }

    public DownloadStub(string url, string filePath, string md5Checksum, WebRequestParams pars, int parallelChunks)
        : this(new Uri(url), filePath, md5Checksum, pars, parallelChunks)
    {
    }

    public DownloadStub(IDownloadStub downloadItem)
        : this(downloadItem.Url,
            downloadItem.FilePath,
            downloadItem.MD5Checksum,
            downloadItem.Pars,
            downloadItem.ParallelRanges)
    {
    }

    public DownloadStub(Uri url, string filePath)
        : this(url, filePath, null, null, 50)
    {
    }

    public DownloadStub(Uri url, string filePath, string md5Checksum, WebRequestParams pars, int parallelChunks)
    {
        MD5Checksum = md5Checksum;
        Url = url;
        FilePath = filePath;
        Pars = pars;
        ParallelRanges = parallelChunks;
        DataLength = -1;
    }

    public void UpdateInstance(IDownloadItem item)
    {
        FilePath = item.FilePath;
        DState = item.DState;
    }

    public Task LoadTargetFileNameAsync() => LoadTargetFileNameAsync(null, null);

    public async Task LoadTargetFileNameAsync(string dirPath, string fallback)
    {
        string fileName = (await Url.GetFileNameAsync() ?? fallback).Guard(nameof(fileName));

        FilePath = Path.Combine(dirPath ?? DirPath, fileName);
    }

    #region IComparable
    public override bool Equals(object obj) => Equals(obj as IDownloadBase);

    public bool Equals(IDownloadBase other) => other is not null && FilePath == other.FilePath && Url == other.Url;

    public override int GetHashCode()
#if NETSTANDARD
    {
        unchecked
        {
            return (FilePath?.GetHashCode() ?? 0) * 397 ^ (Url?.AbsoluteUri?.GetHashCode() ?? 0);
        }
    }
#else
        =>
            HashCode.Combine(FilePath, Url?.AbsoluteUri);
#endif

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