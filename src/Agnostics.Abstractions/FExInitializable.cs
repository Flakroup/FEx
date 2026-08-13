using FEx.Agnostics.Abstractions.Interfaces;
using System.Linq;

namespace FEx.Agnostics.Abstractions;

public abstract class FExInitializable : IFExInitializable
{
    private readonly IFExInitializable[] _dependencies;

    public bool IsInitialized { get; private set; }

    protected FExInitializable(params IFExInitializable[] dependencies)
    {
        _dependencies = dependencies;
    }

    public void Initialize()
    {
        if (IsInitialized)
            return;

        OnInitialize();
        IsInitialized = true;
    }

    protected virtual void OnInitialize()
    {
        foreach (var dependency in _dependencies.Where(dependency => !dependency.IsInitialized))
        {
            if (!dependency.IsInitialized)
                dependency.Initialize();
        }
    }
}