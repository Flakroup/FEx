using FEx.Encryption.Abstractions.Interfaces;
using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Encryption;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExEncryptionModule : IContainer<IFExEncryptionSettings>
{
}