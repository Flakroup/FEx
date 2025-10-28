namespace FEx.Agnostics.Abstractions.Interfaces;

public interface IFExInitializable
{
    bool IsInitialized { get; }

    void Initialize();
}