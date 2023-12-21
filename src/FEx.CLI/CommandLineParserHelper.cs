using CommandLine;
using FEx.Extensions.Collections.Lists;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.CLI;

public static class CommandLineParserHelper
{
    public static T GetConfiguration<T>(string[] args)
    {
        T config = default;
        IList<Error> errors = null;
        Parser.Default.ParseArguments<T>(args)
            .WithParsed(opts => config = opts)
            .WithNotParsed(errs => errors = errs.ToArray());

        return errors.IsNotNullOrEmptyList()
            ? throw new(string.Join(", ", errors.Select(GetErrorInfo)))
            : config;
    }

    private static ErrorData GetErrorData(Error error)
    {
        return error.Tag switch
        {
            ErrorType.BadFormatTokenError => new((BadFormatTokenError)error),
            ErrorType.MissingValueOptionError => new((MissingValueOptionError)error),
            ErrorType.UnknownOptionError => new((UnknownOptionError)error),
            ErrorType.MissingRequiredOptionError => new((MissingRequiredOptionError)error),
            ErrorType.MutuallyExclusiveSetError => new((MutuallyExclusiveSetError)error),
            ErrorType.BadFormatConversionError => new((BadFormatConversionError)error),
            ErrorType.SequenceOutOfRangeError => new((SequenceOutOfRangeError)error),
            ErrorType.RepeatedOptionError => new((RepeatedOptionError)error),
            ErrorType.NoVerbSelectedError => new((NoVerbSelectedError)error),
            ErrorType.BadVerbSelectedError => new((BadVerbSelectedError)error),
            ErrorType.HelpRequestedError => new((HelpRequestedError)error),
            ErrorType.HelpVerbRequestedError => new((HelpVerbRequestedError)error),
            ErrorType.VersionRequestedError => new((VersionRequestedError)error),
            ErrorType.SetValueExceptionError => new((SetValueExceptionError)error),
            ErrorType.InvalidAttributeConfigurationError => new((InvalidAttributeConfigurationError)error),
            ErrorType.MissingGroupOptionError => new((MissingGroupOptionError)error),
            ErrorType.GroupOptionAmbiguityError => new((GroupOptionAmbiguityError)error),
            ErrorType.MultipleDefaultVerbsError => new((MultipleDefaultVerbsError)error),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static string GetErrorInfo(Error error)
    {
        ErrorData e = GetErrorData(error);

        var info = e.Tag.ToString();

        if (e.SetName != null)
            info += $" {e.SetName}";

        if (e.Verb != null)
            info += $" {e.Verb}";

        if (e.NameInfoString != null)
            info += $" {e.NameInfoString}";

        if (e.Exception != null)
            info += $" {e.Exception}";

        return info;
    }
}