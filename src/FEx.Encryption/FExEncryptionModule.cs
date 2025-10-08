using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;

namespace FEx.Encryption;

[Register(typeof(FExEncryption),
    Scope.SingleInstance,
    typeof(FExEncryption),
    typeof(IInitializeModule<IServiceCollection>))]
public class FExEncryptionModule
{
}