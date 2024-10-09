using FEx.DI.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.Fundamentals;

public class FExFundamentals : InitializeModule<IFExFundamentalsContainer>
{

    protected override void AddServices(IFExFundamentalsContainer container, IServiceCollection services) => FExFundamentalsModule.AddServices(container, services);
}