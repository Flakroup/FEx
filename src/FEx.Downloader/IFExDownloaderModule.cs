using FEx.Downloader.Services;
using StrongInject;

namespace FEx.Downloader;

public interface IFExDownloaderModule : IContainer<DownloadService>, IContainer<FExDownloader>
{
}