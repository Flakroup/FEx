using FEx.Json.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StrongInject;

namespace FEx.Json;

public interface IFExJsonModule : IContainer<FExJsonModuleInitializer>, IContainer<DIMeta>,
    IContainer<IContractResolver>, IContainer<JsonSerializerSettings>
{
}