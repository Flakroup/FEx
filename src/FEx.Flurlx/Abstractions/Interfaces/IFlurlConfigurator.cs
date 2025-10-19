using FEx.Agnostics.Abstractions.Interfaces;
using Flurl.Http;
using Polly;
using System.Net.Http;

namespace FEx.Flurlx.Abstractions.Interfaces;

public interface IFlurlConfigurator : IFExInitialize
{
    IFlurlClient GetClient();
    IAsyncPolicy<HttpResponseMessage> GetResiliencePolicy();
}