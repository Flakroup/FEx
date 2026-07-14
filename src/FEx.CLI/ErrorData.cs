using CommandLine;
using System;

namespace FEx.CLI;

public class ErrorData
{
    private NameInfo? _nameInfo;

    public ErrorType Tag { get; }

    public string? NameInfoString { get; private set; }
    public string? Token { get; }
    public string? SetName { get; }
    public string? Verb { get; }
    public Exception? Exception { get; }

    public NameInfo? NameInfo
    {
        get => _nameInfo;
        private set
        {
            _nameInfo = value;

            NameInfoString = NameInfo is not null
                ? GetNameInfoString(NameInfo)
                : null;
        }
    }

    public ErrorData(ErrorType tag, NameInfo? nameInfo, string? token, string? setName, string? verb, Exception? exception)
    {
        Tag = tag;
        NameInfo = nameInfo;
        Token = token;
        SetName = setName;
        Verb = verb;
        Exception = exception;
    }

    public ErrorData(ErrorType tag)
        : this(tag, null, null, null, null, null)
    {
    }

    public ErrorData(ErrorType tag, NameInfo nameInfo)
        : this(tag, nameInfo, null, null, null, null)
    {
    }

    public ErrorData(BadFormatTokenError e)
        : this(e.Tag, null, e.Token, null, null, null)
    {
    }

    public ErrorData(MissingValueOptionError e)
        : this(e.Tag, e.NameInfo, null, null, null, null)
    {
    }

    public ErrorData(UnknownOptionError e)
        : this(e.Tag, null, e.Token, null, null, null)
    {
    }

    public ErrorData(MissingRequiredOptionError e)
        : this(e.Tag, e.NameInfo, null, null, null, null)
    {
    }

    public ErrorData(MutuallyExclusiveSetError e)
        : this(e.Tag, e.NameInfo, null, e.SetName, null, null)
    {
    }

    public ErrorData(BadFormatConversionError e)
        : this(e.Tag, e.NameInfo, null, null, null, null)
    {
    }

    public ErrorData(SequenceOutOfRangeError e)
        : this(e.Tag, e.NameInfo, null, null, null, null)
    {
    }

    public ErrorData(RepeatedOptionError e)
        : this(e.Tag, e.NameInfo, null, null, null, null)
    {
    }

    public ErrorData(NoVerbSelectedError e)
        : this(e.Tag, null, null, null, null, null)
    {
    }

    public ErrorData(BadVerbSelectedError e)
        : this(e.Tag, null, e.Token, null, null, null)
    {
    }

    public ErrorData(HelpRequestedError e)
        : this(e.Tag, null, null, null, null, null)
    {
    }

    public ErrorData(HelpVerbRequestedError e)
        : this(e.Tag, null, null, null, e.Verb, null)
    {
    }

    public ErrorData(VersionRequestedError e)
        : this(e.Tag, null, null, null, null, null)
    {
    }

    public ErrorData(SetValueExceptionError e)
        : this(e.Tag, e.NameInfo, null, null, null, e.Exception)
    {
    }

    public ErrorData(InvalidAttributeConfigurationError e)
        : this(e.Tag, null, null, null, null, null)
    {
    }

    public ErrorData(MissingGroupOptionError e)
        : this(e.Tag, null, null, null, null, null)
    {
    }

    public ErrorData(GroupOptionAmbiguityError e)
        : this(e.Tag, e.NameInfo, null, null, null, null)
    {
    }

    public ErrorData(MultipleDefaultVerbsError e)
        : this(e.Tag, null, null, null, null, null)
    {
    }

    private static string GetNameInfoString(NameInfo nameInfo) =>
        $"{nameInfo.ShortName}|{nameInfo.LongName}|{nameInfo.NameText}";
}