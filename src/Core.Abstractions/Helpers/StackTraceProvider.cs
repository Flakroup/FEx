using FEx.Core.Abstractions.Interfaces;
using System.Diagnostics;

namespace FEx.Core.Abstractions.Helpers;

public class StackTraceProvider : IStackTraceProvider
{
    public StackTrace GetStackTrace() => new(true);
}