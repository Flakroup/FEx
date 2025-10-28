using FEx.Flurlx.Configuration;
using Polly;
using System.Net.Http;

namespace FEx.Flurlx.Services
{
    public interface IFExPollyPolicyBuilder
    {
        IAsyncPolicy<HttpResponseMessage> BuildFullSuitePolicy(PollyPolicyConfiguration config);
    }
}