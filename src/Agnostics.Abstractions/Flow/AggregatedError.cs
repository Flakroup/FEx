using FEx.Agnostics.Abstractions.Interfaces.Flow;
using System.Collections.Generic;

namespace FEx.Agnostics.Abstractions.Flow;

public class AggregatedError : Error
{
    public IReadOnlyCollection<IError> InnerErrors { get; }

    public AggregatedError()
        : this(new List<IError>().AsReadOnly())
    {
    }

    public AggregatedError(IReadOnlyCollection<IError> innerErrors)
        : this(innerErrors, null)
    {
    }

    public AggregatedError(IReadOnlyCollection<IError> innerErrors, string message)
        : base(message)
    {
        InnerErrors = innerErrors;
    }
}