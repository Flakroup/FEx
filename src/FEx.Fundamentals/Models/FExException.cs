using System;

namespace FEx.Fundamentals.Models;

[Serializable]
public class FExException : Exception
{
    public FExException()
    {
    }

    public FExException(string message)
        : base(message)
    {
    }

    public FExException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}