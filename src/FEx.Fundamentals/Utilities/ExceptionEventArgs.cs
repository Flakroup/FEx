using FEx.Extensions.Collections.Lists;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Fundamentals.Utilities;

public class ExceptionEventArgs : EventArgs
{
    public Exception Exception { get; }
    public IDictionary<string, object> Custom { get; }

    public ExceptionEventArgs(Exception ex, params (string, object)[] custom)
        : this(ex,
            custom.IsNotNullOrEmptyList()
                ? custom.ToDictionary(x => x.Item1, x => x.Item2)
                : null)
    {
    }

    public ExceptionEventArgs(Exception ex, IDictionary<string, object> custom = null)
    {
        Exception = ex;
        Custom = custom;
    }
}