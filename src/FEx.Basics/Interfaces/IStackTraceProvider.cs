using System.Diagnostics;

namespace FEx.Basics.Interfaces;

public interface IStackTraceProvider
{
    StackTrace GetStackTrace();
}