using System;
using System.IO;

namespace FEx.PersistentStorage.Abstractions;

public interface IFExDownloadResult : IDisposable
{
    Uri Url { get; }
    string FileName { get; }

    void UseDataStream(Action<MemoryStream> streamAction);
    T UseDataStream<T>(Func<MemoryStream, T> streamAction);
}