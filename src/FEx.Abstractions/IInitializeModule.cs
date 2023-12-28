namespace FEx.Abstractions;

public interface IInitializeModule
{
    bool IsInitialized { get; }

    void Initialize();
}