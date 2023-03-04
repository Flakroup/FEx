namespace FEx.Utilities.StackTraces;

public interface IStackTraceFilter
{
    bool Applies(string logger, string msg);
}