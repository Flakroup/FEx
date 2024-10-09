using StrongInject;

namespace FEx.Platforms.Abstractions.Interfaces;

public interface IFExPlatformsContainer : IContainer<IRegistryService>, IContainer<FExPlatforms>
{
}