using FEx.DI.Abstractions.Interfaces;
using StrongInject;

namespace FEx.Encryption;

[Register(typeof(FExEncryption),
    Scope.SingleInstance,
    typeof(FExEncryption),
    typeof(IInitializeModule))]
public class FExEncryptionModule
{
}