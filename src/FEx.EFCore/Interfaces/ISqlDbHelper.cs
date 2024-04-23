using FEx.Asyncx.Abstractions.Interfaces;

namespace FEx.EFCore.Interfaces;

public interface ISqlDbHelper : IAsyncInitialize
{
    string SQLInstance { get; }
}