using FEx.Agnostics.Abstractions.Interfaces;
using System.Linq;

namespace FEx.Agnostics.Abstractions;

public abstract class FExInitialize : IFExInitialize
{
    private readonly IFExInitialize[] _dependencies;

    public bool IsInitialized { get; private set; }

    protected FExInitialize(params IFExInitialize[] dependencies)
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