namespace FEx.SecureStorage.Abstractions;

public interface ISecureStorageService
{
    T Get<T>(string key);
    void Set(string key, object content);
}
