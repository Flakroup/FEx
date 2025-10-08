using FEx.Agnostics.Utilities;
using LiteDB;

namespace FEx.PersistentStorage.Abstractions;

public interface IDatabaseProvider
{
    ILiteRepository Repository { get; }
    ExtendedReaderWriterLockSlim DbLock { get; }
}