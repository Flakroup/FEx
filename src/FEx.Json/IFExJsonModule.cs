using FEx.Json.Abstractions.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StrongInject;

namespace FEx.Json;

public interface IFExJsonModule : IContainer<FExJsonModuleInitializer>, IContainer<IDIMeta>, IContainer<IContractResolver>, IContainer<JsonSerializerSettings>
{
}