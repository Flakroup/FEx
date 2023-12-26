using System;
using System.IO;

namespace FEx.LiteDbx.Abstractions.Interfaces;

public interface IDownloadResult : IDisposable
{
    Uri Url { get; }
    string FileName { get; }

    void UseDataStream(Action<MemoryStream> streamAction);
    T UseDataStream<T>(Func<MemoryStream, T> streamAction);
}