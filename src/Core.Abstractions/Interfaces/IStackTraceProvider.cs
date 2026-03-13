using System.Diagnostics;

namespace FEx.Core.Abstractions.Interfaces;

public interface IStackTraceProvider
{
    StackTrace GetStackTrace();
}