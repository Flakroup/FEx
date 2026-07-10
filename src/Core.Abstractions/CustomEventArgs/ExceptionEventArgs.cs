using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Core.Abstractions.CustomEventArgs;

public class ExceptionEventArgs : EventArgs
{
    public Exception Exception { get; }
    public IDictionary<string, object>? Custom { get; }

    public ExceptionEventArgs(Exception ex, params (string, object)[] custom)
        : this(ex,
            !custom.IsNullOrEmpty()
                ? custom.ToDictionary(x => x.Item1, x => x.Item2)
                : null)
    {
    }

    public ExceptionEventArgs(Exception ex, IDictionary<string, object>? custom = null)
    {
        Exception = ex;
        Custom = custom;
    }
}