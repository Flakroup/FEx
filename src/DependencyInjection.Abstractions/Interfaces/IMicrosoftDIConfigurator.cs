using Microsoft.Extensions.DependencyInjection;

namespace FEx.DependencyInjection.Abstractions.Interfaces;

//todo split modules MS DI logic to derived types to allow other DI frameworks to be used in the future
public interface IMicrosoftDIConfigurator<in TContainer> : IMicrosoftDIConfigurator
{
    void RegisterServices(IServiceCollection services, TContainer container);
}

public interface IMicrosoftDIConfigurator : IAnyConfigurator
{
    void RegisterServicesUsingContainer(IServiceCollection services, object container);
}