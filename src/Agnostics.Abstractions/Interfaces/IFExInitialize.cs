namespace FEx.Agnostics.Abstractions.Interfaces;

public interface IFExInitialize
{
    bool IsInitialized { get; }

    void Initialize();
}