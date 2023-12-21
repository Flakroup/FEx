using System.Collections.Generic;

namespace FEx.Basics.Flow;

public class AggregatedError : Error
{
    public IReadOnlyCollection<IError> InnerErrors { get; }

    public AggregatedError()
        : this(new List<IError>())
    {
    }

    public AggregatedError(IReadOnlyCollection<IError> innerErrors, string message = null)
        : base(message)
    {
        InnerErrors = innerErrors;
    }
}