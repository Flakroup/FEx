using FEx.Abstractions.Interfaces;
using Flurl.Http;

namespace FEx.Flurlx.Abstractions.Interfaces;

public interface IFlurlConfigurator : IFExInitialize
{
    IFlurlClient GetClient();
}