using FEx.DependencyInjection.Abstractions;
using FEx.Json.Abstractions.Helpers;
using System;
using System.Text.Json.Serialization.Metadata;

namespace FEx.Json.SystemTextJsonx.Resolvers;

/// <summary>
/// The System.Text.Json counterpart of FEx.Json's <c>DIContractResolver</c>, as a <see cref="JsonTypeInfo" /> modifier
/// (see <see cref="FExSystemTextJsonOptions.WithDIConstruction" />). A type registered in DI with an implementation
/// type is created as that implementation (so an interface or abstract service type can be deserialized); a transient
/// registration is resolved from <see cref="FExServiceProvider" /> instead. Singleton and scoped registrations are
/// never handed out, so deserialization cannot overwrite shared state. Unlike the Newtonsoft resolver, the members
/// bound are those of the registered type, not of the implementation: adding members in a modifier requires
/// unreferenced code. The modifier needs no reflection itself and is Native AOT safe.
/// </summary>
public sealed class DIJsonTypeInfoModifier
{
    private readonly DIMeta _diMeta;

    public DIJsonTypeInfoModifier(DIMeta diMeta)
    {
        _diMeta = diMeta ?? throw new ArgumentNullException(nameof(diMeta));
    }

    public void Modify(JsonTypeInfo typeInfo)
    {
        if (typeInfo is null)
            throw new ArgumentNullException(nameof(typeInfo));

        var type = typeInfo.Type;

        if (typeInfo.Kind != JsonTypeInfoKind.Object
            || !_diMeta.IsRegistred(type))
            return;

        // Factory and instance registrations have no implementation type: keep the contract of the type itself.
        if (_diMeta.RegistredTypeFor(type) is { } implementationType
            && implementationType != type)
            UseImplementationContract(typeInfo, implementationType);

        if (_diMeta.IsTransient(type))
            typeInfo.CreateObject = () => FExServiceProvider.Instance.GetRequiredService(type);
    }

    private static void UseImplementationContract(JsonTypeInfo typeInfo, Type implementationType)
    {
        // Only the creator moves over: re-pointing members at the implementation needs
        // JsonTypeInfo.CreateJsonPropertyInfo, which requires unreferenced code, so the members bound stay those of
        // the registered type.
        if (typeInfo.Options.GetTypeInfo(implementationType).CreateObject is { } createObject)
            typeInfo.CreateObject = createObject;
    }
}
