using FEx.EFCore.Helpers;
using FEx.Sqlx.Abstractions;
using StrongInject;

namespace FEx.EFCore;

public interface IFExEFCoreModule : IContainer<ResilientTransaction>, IContainer<ISqlDbHelper>
{
}