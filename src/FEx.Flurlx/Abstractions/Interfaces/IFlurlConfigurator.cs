using FEx.Agnostics.Abstractions.Interfaces;
using Flurl.Http;
using Polly;

namespace FEx.Flurlx.Abstractions.Interfaces;

public interface IFlurlConfigurator : IFExInitializable
{
    IFlurlClient GetClient();
    IAsyncPolicy<IFlurlResponse> GetResiliencePolicy();
}