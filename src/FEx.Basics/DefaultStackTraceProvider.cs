using FEx.Basics.Interfaces;
using System.Diagnostics;

namespace FEx.Basics;

public class DefaultStackTraceProvider : IStackTraceProvider
{
    public StackTrace GetStackTrace() => new(true);
}