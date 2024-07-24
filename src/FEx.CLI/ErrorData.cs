using CommandLine;
using System;

namespace FEx.CLI;

public class ErrorData
{
    private NameInfo _nameInfo;

    public ErrorType Tag { get; }

    public string NameInfoString { get; private set; }
    public string Token { get; }
    public string SetName { get; }
    public string Verb { get; }
    public Exception Exception { get; }

    public NameInfo NameInfo
    {
        get => _nameInfo;
        private set
        {
            _nameInfo = value;

            NameInfoString = NameInfo != null
                ? GetNameInfoString(NameInfo)
                : null;
        }
    }

    public ErrorData(ErrorType tag,
                     NameInfo nameInfo = null,
                     string token = null,
                     string setName = null,
                     string verb = null,
                     Exception exception = null)
    {
        Tag = tag;
        NameInfo = nameInfo;
        Token = token;
        SetName = setName;
        Verb = verb;
        Exception = exception;
    }

    public ErrorData(BadFormatTokenError e)
        : this(e.Tag, token: e.Token)
    {
    }

    public ErrorData(MissingValueOptionError e)
        : this(e.Tag, e.NameInfo)
    {
    }

    public ErrorData(UnknownOptionError e)
        : this(e.Tag, token: e.Token)
    {
    }

    public ErrorData(MissingRequiredOptionError e)
        : this(e.Tag, e.NameInfo)
    {
    }

    public ErrorData(MutuallyExclusiveSetError e)
        : this(e.Tag, e.NameInfo, setName: e.SetName)
    {
    }

    public ErrorData(BadFormatConversionError e)
        : this(e.Tag, e.NameInfo)
    {
    }

    public ErrorData(SequenceOutOfRangeError e)
        : this(e.Tag, e.NameInfo)
    {
    }

    public ErrorData(RepeatedOptionError e)
        : this(e.Tag, e.NameInfo)
    {
    }

    public ErrorData(NoVerbSelectedError e)
        : this(e.Tag)
    {
    }

    public ErrorData(BadVerbSelectedError e)
        : this(e.Tag, token: e.Token)
    {
    }

    public ErrorData(HelpRequestedError e)
        : this(e.Tag)
    {
    }

    public ErrorData(HelpVerbRequestedError e)
        : this(e.Tag, verb: e.Verb)
    {
    }

    public ErrorData(VersionRequestedError e)
        : this(e.Tag)
    {
    }

    public ErrorData(SetValueExceptionError e)
        : this(e.Tag, e.NameInfo, exception: e.Exception)
    {
    }

    public ErrorData(InvalidAttributeConfigurationError e)
        : this(e.Tag)
    {
    }

    public ErrorData(MissingGroupOptionError e)
        : this(e.Tag)
    {
    }

    public ErrorData(GroupOptionAmbiguityError e)
        : this(e.Tag, e.NameInfo)
    {
    }

    public ErrorData(MultipleDefaultVerbsError e)
        : this(e.Tag)
    {
    }

    private static string GetNameInfoString(NameInfo nameInfo) =>
        $"{nameInfo.ShortName}|{nameInfo.LongName}|{nameInfo.NameText}";
}