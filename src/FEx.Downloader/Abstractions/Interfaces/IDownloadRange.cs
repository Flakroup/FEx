using System.Threading.Tasks;

namespace FEx.Downloader.Abstractions.Interfaces;

public interface IDownloadRange : IDownloadBase, IDownloadPart
{
    bool IsConnected { get; }

    Task DoDownloadAsync(int retryCount);
}