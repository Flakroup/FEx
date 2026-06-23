using StrongInject;

namespace FEx.AzureStorage
{
    [Register(typeof(AzureStorageService), Scope.SingleInstance, typeof(IAzureStorageService))]
    public class FExAzureStorageModule
    {
    }

    public interface IFExAzureStorageContainer : IContainer<IAzureStorageService>
    {
    }
}
