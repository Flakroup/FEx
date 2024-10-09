using FEx.DI.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.Legacy;

public class FExLegacy : InitializeModule<IFExLegacyContainer>
{
    /// <inheritdoc />
    protected override void AddServices(IFExLegacyContainer container, IServiceCollection services)
    {
        FExLegacyModule.AddServices(container, services);
    }
}