using FEx.Basics.Utilities;
using LiteDB;

namespace FEx.LiteDbx.Abstractions.Interfaces;

public interface IDatabaseProvider
{
    ILiteRepository Repository { get; }
    ExtendedReaderWriterLockSlim DbLock { get; }
}