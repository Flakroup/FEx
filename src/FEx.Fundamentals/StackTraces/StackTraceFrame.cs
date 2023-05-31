using System;
using System.Diagnostics;
using System.IO;

namespace FEx.Fundamentals.StackTraces;

[DebuggerDisplay("{Namespace} {Type} {Method} {Line}:{Column}")]
[Serializable]
public class StackTraceFrame : IEquatable<StackTraceFrame>
{
    private string _fullFilename;

    private bool? _fileExists;

    public int Column { get; set; }

    public string Filename { get; set; }

    public int Line { get; set; }

    public string Method { get; set; }

    public string Namespace { get; set; }

    public string Type { get; set; }

    public bool FileExists
    {
        get
        {
            if (_fileExists.HasValue)
                return _fileExists.Value;

            return _fullFilename is not null && File.Exists(FullFilename);
        }
        set => _fileExists = value;
    }

    public string FullFilename
    {
        get => _fullFilename;
        set
        {
            _fullFilename = value;
            try
            {
                Filename = Path.GetFileName(value);
            }
            catch (ArgumentException)
            {
                Filename = value;
            }
        }
    }

    public string FullTypeName => string.Concat(Namespace, ".", Type);

    public bool Equals(StackTraceFrame other)
    {
        if (other is null)
            return false;

        if (this == other)
            return true;

        return Equals(other._fullFilename, _fullFilename)
               && Equals(other.Type, Type)
               && other.Line == Line
               && other.Column == Column
               && Equals(other.Filename, Filename)
               && Equals(other.Method, Method)
               && Equals(other.Namespace, Namespace);
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
            return false;

        if (this == obj)
            return true;

        return obj is StackTraceFrame stackTraceFrame && Equals(stackTraceFrame);
    }

    public override int GetHashCode() =>
        ((((((_fullFilename is not null
                 ? _fullFilename.GetHashCode()
                 : 0)
             * 397
             ^ (Type is not null
                 ? Type.GetHashCode()
                 : 0))
            * 397
            ^ Line)
           * 397
           ^ Column)
          * 397
          ^ (Filename is not null
              ? Filename.GetHashCode()
              : 0))
         * 397
         ^ (Method is not null
             ? Method.GetHashCode()
             : 0))
        * 397
        ^ (Namespace is not null
            ? Namespace.GetHashCode()
            : 0);
}