using FEx.Core.Abstractions.Interfaces;
using System.Diagnostics;

namespace FEx.Core.Abstractions.Implementations;

public class DefaultStackTraceProvider : IStackTraceProvider
{
    public StackTrace GetStackTrace() => new(true);
}