using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.PersistentStorage.Abstractions;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExPersistentStorageContainer : IContainer<IDatabaseProvider>, IContainer<ICacheService>,
    IContainer<IFileLocalStorageService>, IContainer<ILocalStorageService>
{
}