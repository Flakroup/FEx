using Microsoft.Extensions.DependencyInjection;

namespace FEx.DependencyInjection.Abstractions.Interfaces;

public interface IMicrosoftDIConfigurator<in TContainer> : IMicrosoftDIConfigurator
{
    void RegisterServices(IServiceCollection services, TContainer container);
}

public interface IMicrosoftDIConfigurator : IAnyConfigurator
{
    void RegisterServicesUsingContainer(IServiceCollection services, object container);
}