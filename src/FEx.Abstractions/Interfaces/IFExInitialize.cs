namespace FEx.Abstractions.Interfaces;

public interface IFExInitialize
{
    bool IsInitialized { get; }

    void Initialize();
}