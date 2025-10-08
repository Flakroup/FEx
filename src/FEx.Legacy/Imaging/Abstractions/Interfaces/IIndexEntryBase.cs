using System;

namespace FEx.Legacy.Imaging.Abstractions.Interfaces;

public interface IIndexEntryBase : IEquatable<IIndexEntryBase>
{
    string AbsoluteUri { get; set; }
    string CheckSum { get; set; }
    string FilePath { get; set; }
    long ResponseContentLength { get; set; }
    int PixelHeight { get; set; }
    int PixelWidth { get; set; }
    Uri LocalUri { get; }

    bool IsEqual(IIndexEntryBase val);
}