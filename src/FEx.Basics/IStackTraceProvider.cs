using System.Diagnostics;

namespace FEx.Basics;

public interface IStackTraceProvider
{
    StackTrace GetStackTrace();
}