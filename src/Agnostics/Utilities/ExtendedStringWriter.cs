using System;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace FEx.Agnostics.Utilities;

public class ExtendedStringWriter : StringWriter
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void FlushedEventHandler(object sender, EventArgs args);

    public event FlushedEventHandler? Flushed;

    public override Encoding Encoding { get; }

    public virtual bool AutoFlush { get; set; }

    public ExtendedStringWriter(StringBuilder builder, bool autoFlush, Encoding desiredEncoding)
        : base(builder)
    {
        AutoFlush = autoFlush;
        Encoding = desiredEncoding;
    }

    public override void Flush()
    {
        base.Flush();
        OnFlush();
    }

    public override void Write(char value) => Run(() => base.Write(value));

    public override void Write(string? value) => Run(() => base.Write(value));

    public override void Write(char[] buffer, int index, int count) => Run(() => base.Write(buffer, index, count));

    protected void OnFlush() => Flushed?.Invoke(this, EventArgs.Empty);

    private void Run(Action action)
    {
        action();

        if (AutoFlush)
            Flush();
    }
}