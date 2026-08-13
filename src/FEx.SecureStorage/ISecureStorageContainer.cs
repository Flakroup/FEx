using FEx.SecureStorage.Abstractions;
using StrongInject;

namespace FEx.SecureStorage;

public interface ISecureStorageContainer : IContainer<ISecureStorageService>
{
}