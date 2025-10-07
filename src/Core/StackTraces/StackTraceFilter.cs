namespace FEx.Fundamentals.StackTraces;

public class StackTraceFilter : IStackTraceFilter
{
    public bool Applies(string logger, string msg) => true;
}