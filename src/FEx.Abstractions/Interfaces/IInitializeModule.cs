namespace FEx.Abstractions.Interfaces;

public interface IInitializeModule
{
    bool IsInitialized { get; }

    void Initialize();
}