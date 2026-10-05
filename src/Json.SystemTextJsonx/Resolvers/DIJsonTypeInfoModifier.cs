using FEx.DependencyInjection.Abstractions;
using FEx.Json.Abstractions;
using FEx.Json.Abstractions.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json.Serialization.Metadata;

namespace FEx.Json.SystemTextJsonx.Resolvers;

/// <summary>
/// The System.Text.Json counterpart of FEx.Json's <c>DIContractResolver</c>, as a <see cref="JsonTypeInfo" /> modifier
/// (see <see cref="FExSystemTextJsonOptions.WithDIConstruction" />).
/// <list type="bullet">
///   <item>A registration whose <see cref="ServiceLifetime" /> is <see cref="ServiceLifetime.Transient" /> is resolved
///   from <see cref="FExServiceProvider" /> once per payload. "Transient" is what the registration says, not what the
///   container does: a service registered through <c>AddTransientServiceUsingContainer</c> on a StrongInject
///   <c>SingleInstance</c> registration is a shared instance, and the payload is written into it. Register a type you
///   deserialize as truly transient, or not at all.</item>
///   <item>Any other registration with an implementation type is created as that implementation through its
///   parameterless constructor (so an interface or abstract service type can be deserialized); a singleton or scoped
///   instance is never handed out, so deserialization cannot overwrite shared state. An implementation without a
///   parameterless constructor fails with <see cref="FExJsonException" />.</item>
///   <item>Factory and instance registrations keep the contract of the registered type.</item>
/// </list>
/// Unlike the Newtonsoft resolver, the members bound are those of the registered type, not of the implementation:
/// adding members in a modifier requires unreferenced code. The modifier itself needs no reflection, but the transient
/// path resolves through <see cref="FExServiceProvider" />, which uses reflection (<c>MakeGenericMethod</c>) and is not
/// trim- or AOT-analyzed; a Native AOT application should not rely on transient DI construction.
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
        var createObject = typeInfo.Options.GetTypeInfo(implementationType).CreateObject;
        var type = typeInfo.Type;

        // Without a parameterless constructor the implementation cannot be built here; a transient registration is
        // built through DI below, anything else fails with a message that says why instead of STJ's generic one.
        typeInfo.CreateObject = createObject
                                ?? (() => throw new FExJsonException(
                                    $"{type.FullName} is registered with {implementationType.FullName}, which has no parameterless constructor; register it as transient to build it through DI."));
    }
}
