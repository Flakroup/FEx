using FEx.DependencyInjection.Abstractions.Interfaces;
using StrongInject;

namespace FEx.Encryption;

[Register(typeof(FExEncryptionModuleInitializer),
    Scope.SingleInstance,
    typeof(FExEncryptionModuleInitializer),
    typeof(IInitializeModule))]
public class FExEncryptionModule
{
}