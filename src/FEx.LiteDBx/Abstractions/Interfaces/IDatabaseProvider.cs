using FEx.Basics.Utilities;
using LiteDB;

namespace FEx.LiteDBx.Abstractions.Interfaces;

public interface IDatabaseProvider
{
    ILiteRepository Repository { get; }
    ExtendedReaderWriterLockSlim DbLock { get; }
}