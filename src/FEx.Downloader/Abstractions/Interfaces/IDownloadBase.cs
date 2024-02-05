using FEx.Downloader.Enums;
using FEx.Extensions.Base.Models;
using System;

namespace FEx.Downloader.Abstractions.Interfaces;

public interface IDownloadBase : IEquatable<IDownloadBase>, IComparable, IComparable<IDownloadBase>
{
    string FilePath { get; }
    Uri Url { get; }
    string DirPath { get; }
    DownloadState DState { get; }
    WebRequestParams Pars { get; }
}