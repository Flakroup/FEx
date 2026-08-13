using FEx.Core.Abstractions.Interfaces;

namespace FEx.Sqlx.Abstractions;

public interface ISqlDbHelper : IAsyncInitializable
{
    string? SQLInstance { get; }
}