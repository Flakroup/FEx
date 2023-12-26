using FEx.Utilities.Basics;
using LiteDB;

namespace FEx.LiteDbx.Abstractions.Interfaces;

public interface IDatabaseProvider
{
    ILiteRepository Repository { get; }
    ExtendedReaderWriterLockSlim DbLock { get; }
}