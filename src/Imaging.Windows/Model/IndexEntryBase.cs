using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.BaseObjects;
using FEx.Legacy.Imaging.Abstractions.Interfaces;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FEx.Imaging.Windows.Model;

public class IndexEntryBase : NotifyPropertyChanged, IIndexEntryBase
{
    // EF entity backing fields for IIndexEntryBase's non-nullable string/Uri contract; populated by EF/loader, transiently null before assignment
    private string _absoluteUri = null!;
    private long _responseContentLength;
    private string _filePath = null!;
    private string _checkSum = null!;
    private int _pixelHeight;
    private int _pixelWidth;
    private Uri _localUri = null!;

    [Key]
    [StringLength(1024)]
    public string AbsoluteUri
    {
        get => _absoluteUri;
        set
        {
            if (SetProperty(ref _absoluteUri, value))
                OnAbsoluteUriChange();
        }
    }

    [StringLength(1024)]
    public string CheckSum
    {
        get => _checkSum;
        set
        {
            if (SetProperty(ref _checkSum, value))
            {
            }
        }
    }

    [StringLength(1024)]
    public string FilePath
    {
        get => _filePath;
        set
        {
            if (SetProperty(ref _filePath, value))
                OnFilePathChange();
        }
    }

    public long ResponseContentLength
    {
        get => _responseContentLength;
        set
        {
            if (SetProperty(ref _responseContentLength, value))
            {
            }
        }
    }

    public int PixelHeight
    {
        get => _pixelHeight;
        set => SetProperty(ref _pixelHeight, value);
    }

    public int PixelWidth
    {
        get => _pixelWidth;
        set => SetProperty(ref _pixelWidth, value);
    }

    [JsonIgnore]
    [NotMapped]
    public Uri LocalUri
    {
        get => _localUri;
        protected set => SetProperty(ref _localUri, value);
    }

    public bool Equals(IIndexEntryBase? other) => other is not null && IsEqual(other);

    public virtual bool IsEqual(IIndexEntryBase val) =>
        AbsoluteUri.IsBothNullOrEqual(val.AbsoluteUri)
        && CheckSum.IsBothNullOrEqual(val.CheckSum)
        && FilePath.IsBothNullOrEqual(val.FilePath)
        && ResponseContentLength == val.ResponseContentLength;

    protected virtual void OnAbsoluteUriChange()
    {
    }

    protected virtual void OnFilePathChange()
    {
    }
}