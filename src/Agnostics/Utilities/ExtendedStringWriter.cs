using System;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace FEx.Agnostics.Utilities;

/// <summary>A <see cref="StringWriter"/> with a configurable encoding, optional flush after every write, and a notification when flushed.</summary>
public class ExtendedStringWriter : StringWriter
{
    /// <summary>Represents the method that handles the <see cref="Flushed"/> event.</summary>
    /// <param name="sender">The writer that was flushed.</param>
    /// <param name="args">The event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void FlushedEventHandler(object sender, EventArgs args);

    /// <summary>Occurs after the writer has been flushed.</summary>
    public event FlushedEventHandler? Flushed;

    /// <summary>Gets the encoding reported by the writer, as supplied at construction.</summary>
    public override Encoding Encoding { get; }

    /// <summary>Gets or sets a value indicating whether the writer flushes after every write.</summary>
    public virtual bool AutoFlush { get; set; }

    /// <summary>Initializes a writer over a string builder.</summary>
    /// <param name="builder">The builder that receives the written text.</param>
    /// <param name="autoFlush">Whether to flush after every write.</param>
    /// <param name="desiredEncoding">The encoding the writer reports.</param>
    public ExtendedStringWriter(StringBuilder builder, bool autoFlush, Encoding desiredEncoding)
        : base(builder)
    {
        AutoFlush = autoFlush;
        Encoding = desiredEncoding;
    }

    /// <summary>Flushes the writer and raises <see cref="Flushed"/>.</summary>
    public override void Flush()
    {
        base.Flush();
        OnFlush();
    }

    /// <summary>Writes a character and flushes when <see cref="AutoFlush"/> is on.</summary>
    /// <param name="value">The character to write.</param>
    public override void Write(char value) => Run(() => base.Write(value));

    /// <summary>Writes a string and flushes when <see cref="AutoFlush"/> is on.</summary>
    /// <param name="value">The string to write.</param>
    public override void Write(string? value) => Run(() => base.Write(value));

    /// <summary>Writes part of a character array and flushes when <see cref="AutoFlush"/> is on.</summary>
    /// <param name="buffer">The array to read characters from.</param>
    /// <param name="index">The position in <paramref name="buffer"/> at which to start reading.</param>
    /// <param name="count">The number of characters to write.</param>
    public override void Write(char[] buffer, int index, int count) => Run(() => base.Write(buffer, index, count));

    /// <summary>Raises the <see cref="Flushed"/> event.</summary>
    protected void OnFlush() => Flushed?.Invoke(this, EventArgs.Empty);

    private void Run(Action action)
    {
        action();

        if (AutoFlush)
            Flush();
    }
}