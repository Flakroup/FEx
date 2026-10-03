namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>An object that is initialized explicitly and only once.</summary>
public interface IFExInitializable
{
    /// <summary>Gets a value indicating whether the object has been initialized.</summary>
    bool IsInitialized { get; }

    /// <summary>Initializes the object; does nothing when it is already initialized.</summary>
    void Initialize();
}