using FEx.Downloader.Abstractions.Interfaces;
using System.Threading.Tasks;

namespace FEx.Downloader.Extensions;

public static class DownloadRangeExtensions
{
    public static Task DoDownloadAsync(this IDownloadRange downloadRange) =>
        downloadRange.DoDownloadAsync(3);
}
