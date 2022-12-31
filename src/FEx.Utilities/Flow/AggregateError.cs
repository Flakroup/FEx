using System.Collections.Generic;

namespace FEx.Utilities.Flow;

public class AggregateError : Error
{
    public ICollection<Error> InnerErrors { get; }

    public AggregateError()
        : this(new List<Error>())
    {
    }

    public AggregateError(ICollection<Error> innerErrors, string message = null)
    {
        InnerErrors = innerErrors;
        Message = message;
    }
}