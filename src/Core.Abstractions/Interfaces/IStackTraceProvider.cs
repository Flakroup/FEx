using System.Diagnostics;

namespace FEx.Abstractions.Interfaces;

public interface IStackTraceProvider
{
    StackTrace GetStackTrace();
}