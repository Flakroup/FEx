namespace FEx.Fundamentals.StackTraces;

public interface IStackTraceFilter
{
    bool Applies(string logger, string msg);
}