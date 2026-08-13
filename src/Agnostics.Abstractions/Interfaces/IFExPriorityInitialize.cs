namespace FEx.Agnostics.Abstractions.Interfaces;

public interface IFExPriorityInitialize : IFExInitializable
{
    /// <summary>
    /// Priority of initialization - lower value is higher priority
    /// </summary>
    int Priority { get; }
}