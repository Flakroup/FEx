using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Encryption.Abstractions;
using FEx.Encryption.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;

namespace FEx.Encryption;

[Register(typeof(FExEncryption),
    Scope.SingleInstance,
    typeof(FExEncryption),
    typeof(IFExPriorityInitialize),
    typeof(IInitializeModule<IServiceCollection>))]
[Register(typeof(FExEncryptionSettings), Scope.SingleInstance, typeof(IFExEncryptionSettings))]
public class FExEncryptionModule
{
}