using FEx.Abstractions.Interfaces;

namespace FEx.EFCore.Interfaces;

public interface ISqlDbHelper : IAsyncInitializable
{
    string SQLInstance { get; }
}