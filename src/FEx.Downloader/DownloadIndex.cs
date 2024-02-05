using FEx.Downloader.Abstractions.Interfaces;
using System;

namespace FEx.Downloader;

public class DownloadIndex : IEquatable<DownloadIndex>
{
    public string Url { get; }
    public string FilePath { get; }

    public DownloadIndex(IDownloadBase di)
    {
        Url = di.Url.AbsoluteUri;
        FilePath = di.FilePath;
    }

    public bool Equals(DownloadIndex other)
    {
        if (other is null)
            return false;

        return ReferenceEquals(this, other)
               || string.Equals(Url, other.Url, StringComparison.OrdinalIgnoreCase)
               && string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);
    }

    public static bool operator ==(DownloadIndex left, DownloadIndex right) => Equals(left, right);

    public static bool operator !=(DownloadIndex left, DownloadIndex right) => !Equals(left, right);

    public override string ToString() => $"{Url}\t{FilePath}";

    public override bool Equals(object obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        return obj.GetType() == typeof(DownloadIndex) && Equals((DownloadIndex)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Url is not null
                       ? StringComparer.OrdinalIgnoreCase.GetHashCode(Url)
                       : 0)
                   * 397
                   ^ (FilePath is not null
                       ? StringComparer.OrdinalIgnoreCase.GetHashCode(FilePath)
                       : 0);
        }
    }
}