using StrongInject;

namespace FEx.NuGetx;

public interface INuGetExModule : IContainer<NuGetEx>, IContainer<NuGetManager>, IContainer<NuGetLogger<NuGetManager>>
{
}