using FEx.Abstractions.Interfaces;
using System.Diagnostics;

namespace FEx.Abstractions.Implementations;

public class DefaultStackTraceProvider : IStackTraceProvider
{
    public StackTrace GetStackTrace() => new(true);
}