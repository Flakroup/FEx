using System;

namespace FEx.Utilities.StackTraces;

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

        if (obj.GetType() != typeof(StackTraceInfo))
            return false;

        return Equals((StackTraceInfo)obj);
    }

    public override int GetHashCode()
    {
        if (Frames is null)
            return 0;

        return Frames.GetHashCode();
    }
}