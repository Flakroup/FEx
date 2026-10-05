using FEx.Json.Abstractions;
using FEx.Json.Abstractions.Helpers;
using StrongInject;
using System.Text.Json;

namespace FEx.Json.SystemTextJsonx;

public interface IFExSystemTextJsonContainer : IContainer<DIMeta>, IContainer<JsonSerializerOptions>,
    IContainer<IFExJsonSerializer>
{
}
