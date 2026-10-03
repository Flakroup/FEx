namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>An initializable object that declares the order in which it should be initialized.</summary>
public interface IFExPriorityInitialize : IFExInitializable
{
    /// <summary>
    /// Priority of initialization - lower value is higher priority
    /// </summary>
    int Priority { get; }
}