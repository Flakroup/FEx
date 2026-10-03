using FEx.Agnostics.Abstractions.Interfaces.Flow;
using System.Collections.Generic;

namespace FEx.Agnostics.Abstractions.Flow;

/// <summary>An error that groups several inner errors.</summary>
public class AggregatedError : Error
{
    /// <summary>Gets the grouped errors.</summary>
    public IReadOnlyCollection<IError> InnerErrors { get; }

    /// <summary>Initializes an aggregated error with no inner errors.</summary>
    public AggregatedError()
        : this(new List<IError>().AsReadOnly())
    {
    }

    /// <summary>Initializes an aggregated error</summary>
    /// <param name="innerErrors">The errors to group.</param>
    public AggregatedError(IReadOnlyCollection<IError> innerErrors)
        : this(innerErrors, null)
    {
    }

    /// <summary>Initializes an aggregated error with a message</summary>
    /// <param name="innerErrors">The errors to group.</param>
    /// <param name="message">The error message.</param>
    public AggregatedError(IReadOnlyCollection<IError> innerErrors, string? message)
        : base(message)
    {
        InnerErrors = innerErrors;
    }
}