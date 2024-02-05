namespace FEx.Platforms.Abstractions;

public class Platform
{
    public static IRegistryService RegistryService { get; private set; }

    public static void Initialize(IRegistryService registryService)
    {
        RegistryService = registryService;
    }
}