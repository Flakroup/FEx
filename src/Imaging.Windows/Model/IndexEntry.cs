using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;

namespace FEx.Imaging.Windows.Model;

#pragma warning disable IDISP025 // has protected members for subclassing
public class IndexEntry : IndexEntryBase, IDisposable
#pragma warning restore IDISP025
{
    private Uri? _url;
    private CachedImage? _cachedImage;
    private string? _filesCacheRootDirPath;
    private FileInfo? _cache;
    private string? _extension;
    private string? _fileName;
    private bool _isDownloading;
    private IIndexEntryConfig? _config;

    [JsonIgnore]
    [NotMapped]
    public IIndexEntryConfig? Config
    {
        get => _config;
        set
        {
            if (SetProperty(ref _config, value))
                FilesCacheRootDirPath = value?.FilesCacheDirPath;
        }
    }

    [JsonIgnore]
    [NotMapped]
    public Uri? Url
    {
        get => _url;
        protected set
        {
            if (SetProperty(ref _url, value))
            {
                // AbsoluteUri (IIndexEntryBase [Key]) is non-nullable by contract but this entity stores null on reset
                AbsoluteUri = (Url?.AbsoluteUri)!;

#pragma warning disable IDISP003 // semaphore from LockSrv, not owned
                Semaphore = AbsoluteUri is not null
                    ? LockSrv.EnsureLock(AbsoluteUri)
                    : null;
#pragma warning restore IDISP003

                FileName = GetFileName(Url);

                RefreshCachedImage();
            }
        }
    }

    [JsonIgnore]
    [NotMapped]
    public string? FilesCacheRootDirPath
    {
        get => _filesCacheRootDirPath;
        protected set
        {
            if (SetProperty(ref _filesCacheRootDirPath, value))
            {
                RefreshCachedImage();

                // FilesCacheRootDirPath! preserves prior throw-on-null behavior; FilePath's directory is non-null when FilePath is a full path
                if (FilePath?.StartsWith(FilesCacheRootDirPath!) == false)
                    FilePath = FilePath.Replace(Path.GetDirectoryName(FilePath)!, FilesCacheRootDirPath!);
            }
        }
    }

    [JsonIgnore]
    [NotMapped]
    public CachedImage? CachedImage
    {
        get => _cachedImage;
        protected set => SetProperty(ref _cachedImage, value);
    }

    [JsonIgnore]
    [NotMapped]
    public FileInfo? Cache
    {
        get => _cache;
        protected set
        {
            if (SetProperty(ref _cache, value))
                RefreshFileInfo();
        }
    }

    [JsonIgnore]
    [NotMapped]
    public string? FileName
    {
        get => _fileName;
        protected set => SetProperty(ref _fileName, value);
    }

    [JsonIgnore]
    [NotMapped]
    public string? Extension
    {
        get => _extension;
        protected internal set => SetProperty(ref _extension, value);
    }

    [JsonIgnore]
    [NotMapped]
    public bool IsDownloading
    {
        get => _isDownloading;
        protected internal set => SetProperty(ref _isDownloading, value);
    }

#pragma warning disable IDISP008 // semaphore from LockSrv, ownership managed externally
    [JsonIgnore]
    [NotMapped]
    protected internal SemaphoreSlim? Semaphore { get; protected set; }
#pragma warning restore IDISP008

    private static ISynchronizedAccessService LockSrv => FExCoreStatics.SynchronizedAccessService;

    public IndexEntry()
    {
    }

    public IndexEntry(string absoluteUri, IIndexEntryConfig config, string? fileName = null)
        : this()
    {
        Config = config;
        AbsoluteUri = absoluteUri;

        if (fileName is not null)
            // Config assigned above sets FilesCacheRootDirPath from the config's non-null cache dir
            FilePath = Path.Combine(FilesCacheRootDirPath!, fileName);
    }

    public static string? GetFileName(Uri? url) => GetFileName(url?.AbsoluteUri);

    public static string? GetFileName(string? uri) =>
        uri is not null
            ? uri.GenerateMd5OfString()
            : null;

    protected override void OnAbsoluteUriChange() =>
        Url = AbsoluteUri.IsNotNullOrEmptyString()
            ? new Uri(AbsoluteUri)
            : null;

    protected override void OnFilePathChange() => Refresh();

    internal void Refresh()
    {
        if (FilesCacheRootDirPath.IsNullOrEmptyString()
            || FilePath?.StartsWith(FilesCacheRootDirPath) == false)
            FilesCacheRootDirPath = Path.GetDirectoryName(FilePath);

        if (FilePath is not null)
            ResetCacheFile(FilePath);
        else
            Cache = null;
    }

    internal void RefreshFileInfo()
    {
        Cache?.Refresh();

        if (Cache is not null)
            FileName = Path.GetFileNameWithoutExtension(Cache.Name);

        // CheckSum/FilePath/LocalUri are non-nullable per IIndexEntryBase; this entity legitimately clears them to null, so suppress
        CheckSum = (Cache?.GenerateMd5OfFile())!;

        Extension = Cache?.Exists == true
            ? Cache.Extension
            : null;

        FilePath = (Cache?.Exists == true
            ? Cache.FullName
            : null)!;

        LocalUri = (FilePath is not null
            ? new Uri($"file:///{FilePath}", UriKind.Absolute)
            : null)!;

        TrySetSize();
    }

    internal void ResetCache()
    {
        Extension = null;
        ResponseContentLength = -1;
    }

    internal void ResetCacheFile(string filePath)
    {
        if (Cache?.FullName != filePath)
            Cache = new(filePath);

        RefreshFileInfo();
    }

    private void RefreshCachedImage()
    {
        CachedImage?.Dispose();

        if (Url is not null
            && FilesCacheRootDirPath is not null)
        {
            if (CachedImage is null)
                CachedImage = new(this);
            else
                CachedImage.OnParentConfigurationChange();
        }
        else if (CachedImage is not null)
        {
            CachedImage = null;
        }
    }

    private void TrySetSize()
    {
        try
        {
            if (Cache?.Exists == true
                && Cache.Extension != ".svg")
            {
                using var imageStream = Cache.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                var decoder = BitmapDecoder.Create(imageStream,
                    BitmapCreateOptions.IgnoreColorProfile,
                    BitmapCacheOption.Default);

                PixelHeight = decoder.Frames[0].PixelHeight;
                PixelWidth = decoder.Frames[0].PixelWidth;
            }
        }
        catch
        {
            //ignored
        }
    }

    #region IDisposable
    public void Dispose()
    {
        Semaphore?.Dispose();
        CachedImage?.Dispose();
    }
    #endregion
}