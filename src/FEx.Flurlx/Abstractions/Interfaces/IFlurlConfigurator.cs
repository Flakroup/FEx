using Flurl.Http;

namespace FEx.Flurlx.Abstractions.Interfaces;

public interface IFlurlConfigurator
{
    void Configure();
    IFlurlClient GetClient();
}