using FEx.EFCore.Helpers;
using StrongInject;

namespace FEx.EFCore;

public interface IFExEFCoreModule : IContainer<ResilientTransaction>
{
}