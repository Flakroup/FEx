using StrongInject;

namespace FEx.MVVM.Abstractions.Interfaces;

public interface IFExMvvmModule : IContainer<FExMvvmModuleInitializer>, IContainer<IMessagePopupService>
{
}