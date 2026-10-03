using FEx.Agnostics.Abstractions.Interfaces;
using System.Linq;

namespace FEx.Agnostics.Abstractions;

/// <summary>Base class for objects that initialize once, after initializing the dependencies they were given.</summary>
public abstract class FExInitializable : IFExInitializable
{
    private readonly IFExInitializable[] _dependencies;

    /// <inheritdoc />
    public bool IsInitialized { get; private set; }

    /// <summary>Initializes the instance.</summary>
    /// <param name="dependencies">Objects that are initialized before this one when <see cref="OnInitialize" /> runs.</param>
    protected FExInitializable(params IFExInitializable[] dependencies)
    {
        _dependencies = dependencies;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        if (IsInitialized)
            return;

        OnInitialize();
        IsInitialized = true;
    }

    /// <summary>Initializes the dependencies that are not initialized yet; override to add initialization logic and call the base implementation to keep that behavior.</summary>
    protected virtual void OnInitialize()
    {
        foreach (var dependency in _dependencies.Where(dependency => !dependency.IsInitialized))
        {
            if (!dependency.IsInitialized)
                dependency.Initialize();
        }
    }
}