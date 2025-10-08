using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using StrongInject;

namespace FEx.EFCore;

public interface IFExEFCoreModule : IContainer<ResilientTransaction>, IContainer<ISqlDbHelper>
{
}