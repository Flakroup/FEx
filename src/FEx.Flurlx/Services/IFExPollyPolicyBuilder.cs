using FEx.Flurlx.Configuration;
using Flurl.Http;
using Polly;

namespace FEx.Flurlx.Services;

public interface IFExPollyPolicyBuilder
{
    IAsyncPolicy<IFlurlResponse> BuildFullSuitePolicy(PollyPolicyConfiguration config);
}