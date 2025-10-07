namespace FEx.Abstractions.Interfaces;

public interface IFExPriorityInitialize : IFExInitialize
{
    /// <summary>
    /// Priority of initialization - lower value is higher priority
    /// </summary>
    int Priority { get; }
}