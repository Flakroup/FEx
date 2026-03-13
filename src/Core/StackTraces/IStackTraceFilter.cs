namespace FEx.Core.StackTraces;

public interface IStackTraceFilter
{
    bool Applies(string logger, string msg);
}