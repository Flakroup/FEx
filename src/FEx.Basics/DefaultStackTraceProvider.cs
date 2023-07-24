using System.Diagnostics;
using FEx.Basics.Interfaces;

namespace FEx.Basics;

public class DefaultStackTraceProvider : IStackTraceProvider
{
    public StackTrace GetStackTrace() => new(true);
}