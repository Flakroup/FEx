using FEx.Abstractions.Interfaces;

namespace FEx.Abstractions;

public abstract class InitializeModule : IInitializeModule
{
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        if (IsInitialized)
            return;

        OnInitialize();
        IsInitialized = true;
    }

    protected abstract void OnInitialize();
}