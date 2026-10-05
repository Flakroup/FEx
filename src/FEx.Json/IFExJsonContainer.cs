using FEx.Json.Abstractions;
using FEx.Json.Abstractions.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StrongInject;

namespace FEx.Json;

public interface IFExJsonContainer : IContainer<FExJson>, IContainer<DIMeta>, IContainer<IContractResolver>,
    IContainer<JsonSerializerSettings>, IContainer<IFExJsonSerializer>
{
}