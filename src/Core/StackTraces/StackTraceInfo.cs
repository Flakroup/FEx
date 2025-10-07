using System;

namespace FEx.Fundamentals.StackTraces;

[Serializable]
public class StackTraceInfo : IEquatable<StackTraceInfo>
{
    public StackTraceFrame[] Frames { get; set; }

    public bool Equals(StackTraceInfo other)
    {
        if (other is null)
            return false;

        if (this == other)
            return true;

        if (Frames.Length != other.Frames.Length)
            return false;

        for (var i = 0; i < Frames.Length; i++)
        {
            if (!Equals(Frames[i], other.Frames[i]))
                return false;
        }

        return true;
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
            return false;

        if (this == obj)
            return true;

        return obj.GetType() == typeof(StackTraceInfo) && Equals((StackTraceInfo)obj);
    }

    public override int GetHashCode() =>
        Frames is null
            ? 0
            : Frames.GetHashCode();
}