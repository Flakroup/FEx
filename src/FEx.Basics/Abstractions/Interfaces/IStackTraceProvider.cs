using System.Diagnostics;

namespace FEx.Basics.Abstractions.Interfaces;

public interface IStackTraceProvider
{
    StackTrace GetStackTrace();
}