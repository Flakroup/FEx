using CommandLine;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FEx.CLI.Tests;

/// <summary>ErrorData flattens every CommandLineParser error into one shape: tag, token, set, verb, name info, exception.</summary>
public sealed class ErrorDataTests
{
    // NameInfo has no public constructor; take a real one from a parse error for --name / -n.
    private static NameInfo Name => Single<MissingRequiredOptionError>(typeof(CliFixtures.Simple)).NameInfo;

    [Fact]
    public void TagOnlyConstructor_LeavesEverythingElseEmpty()
    {
        ErrorData data = new(ErrorType.NoVerbSelectedError);

        data.Tag.ShouldBe(ErrorType.NoVerbSelectedError);
        data.NameInfo.ShouldBeNull();
        data.NameInfoString.ShouldBeNull();
        data.Token.ShouldBeNull();
        data.SetName.ShouldBeNull();
        data.Verb.ShouldBeNull();
        data.Exception.ShouldBeNull();
    }

    [Fact]
    public void NameInfoConstructor_RendersTheNameInfoAsShortLongAndText()
    {
        ErrorData data = new(ErrorType.MissingValueOptionError, Name);

        data.NameInfo.ShouldBe(Name);
        data.NameInfoString.ShouldBe("n|name|n, name");
    }

    [Fact]
    public void FullConstructor_KeepsEveryField()
    {
        InvalidOperationException exception = new("boom");

        ErrorData data = new(ErrorType.SetValueExceptionError, Name, "tok", "set", "verb", exception);

        data.Tag.ShouldBe(ErrorType.SetValueExceptionError);
        data.NameInfoString.ShouldBe("n|name|n, name");
        data.Token.ShouldBe("tok");
        data.SetName.ShouldBe("set");
        data.Verb.ShouldBe("verb");
        data.Exception.ShouldBeSameAs(exception);
    }

    [Fact]
    public void FullConstructor_WithoutNameInfo_HasNoNameInfoString()
    {
        ErrorData data = new(ErrorType.UnknownOptionError, null, "tok", null, null, null);

        data.NameInfo.ShouldBeNull();
        data.NameInfoString.ShouldBeNull();
        data.Token.ShouldBe("tok");
    }

    [Fact]
    public void BadFormatTokenError_KeepsTheToken()
    {
        var error = Single<BadFormatTokenError>(typeof(CliFixtures.Optional), "--name=");

        error.Token.ShouldNotBeNullOrEmpty();
        Check(new ErrorData(error), ErrorType.BadFormatTokenError, token: error.Token);
    }

    [Fact]
    public void MissingValueOptionError_KeepsTheNameInfo() =>
        Check(new ErrorData(Single<MissingValueOptionError>(typeof(CliFixtures.Optional), "--tag")),
            ErrorType.MissingValueOptionError,
            nameInfoString: "|tag|tag");

    [Fact]
    public void UnknownOptionError_KeepsTheToken() =>
        Check(new ErrorData(Single<UnknownOptionError>(typeof(CliFixtures.Optional), "--zzz")),
            ErrorType.UnknownOptionError,
            token: "zzz");

    [Fact]
    public void MissingRequiredOptionError_KeepsTheNameInfo() =>
        Check(new ErrorData(Single<MissingRequiredOptionError>(typeof(CliFixtures.Simple))),
            ErrorType.MissingRequiredOptionError,
            nameInfoString: "n|name|n, name");

    [Fact]
    public void MutuallyExclusiveSetError_KeepsTheNameInfoAndTheSetName()
    {
        var errors = Errors(typeof(CliFixtures.Exclusive), "--aa", "1", "--bb", "2").OfType<MutuallyExclusiveSetError>().ToList();

        errors.Count.ShouldBe(2);

        var data = new ErrorData(errors[0]);

        data.Tag.ShouldBe(ErrorType.MutuallyExclusiveSetError);
        data.SetName.ShouldBe(errors[0].SetName);
        data.SetName.ShouldNotBeNullOrEmpty();
        data.NameInfoString.ShouldBe($"{errors[0].NameInfo.ShortName}|{errors[0].NameInfo.LongName}|{errors[0].NameInfo.NameText}");
    }

    [Fact]
    public void BadFormatConversionError_KeepsTheNameInfo() =>
        Check(new ErrorData(Single<BadFormatConversionError>(typeof(CliFixtures.Optional), "--port", "abc")),
            ErrorType.BadFormatConversionError,
            nameInfoString: "p|port|p, port");

    [Fact]
    public void SequenceOutOfRangeError_KeepsTheNameInfo() =>
        Check(new ErrorData(Single<SequenceOutOfRangeError>(typeof(CliFixtures.Optional), "--pair", "only-one")),
            ErrorType.SequenceOutOfRangeError,
            nameInfoString: "|pair|pair");

    [Fact]
    public void RepeatedOptionError_KeepsTheNameInfo() =>
        Check(new ErrorData(Single<RepeatedOptionError>(typeof(CliFixtures.Optional), "--port", "1", "--port", "2")),
            ErrorType.RepeatedOptionError,
            nameInfoString: "p|port|p, port");

    [Fact]
    public void NoVerbSelectedError_CarriesOnlyTheTag() =>
        Check(new ErrorData(Single<NoVerbSelectedError>(typeof(CliFixtures.RunVerb), typeof(CliFixtures.BuildVerb))),
            ErrorType.NoVerbSelectedError);

    [Fact]
    public void BadVerbSelectedError_KeepsTheToken() =>
        Check(new ErrorData(Single<BadVerbSelectedError>(typeof(CliFixtures.RunVerb), typeof(CliFixtures.BuildVerb), "nope")),
            ErrorType.BadVerbSelectedError,
            token: "nope");

    [Fact]
    public void HelpRequestedError_CarriesOnlyTheTag() =>
        Check(new ErrorData(Single<HelpRequestedError>(typeof(CliFixtures.Simple), "--help")), ErrorType.HelpRequestedError);

    [Fact]
    public void HelpVerbRequestedError_KeepsTheVerb() =>
        Check(new ErrorData(Single<HelpVerbRequestedError>(typeof(CliFixtures.RunVerb), typeof(CliFixtures.BuildVerb), "help", "run")),
            ErrorType.HelpVerbRequestedError,
            verb: "run");

    [Fact]
    public void VersionRequestedError_CarriesOnlyTheTag() =>
        Check(new ErrorData(Single<VersionRequestedError>(typeof(CliFixtures.Simple), "--version")),
            ErrorType.VersionRequestedError);

    [Fact]
    public void SetValueExceptionError_KeepsTheNameInfoAndTheException()
    {
        var error = Single<SetValueExceptionError>(typeof(CliFixtures.Throwing), "--boom", "x");

        var data = new ErrorData(error);

        Check(data, ErrorType.SetValueExceptionError, nameInfoString: "|boom|boom", exception: error.Exception);
        data.Exception.ShouldNotBeNull();
    }

    [Fact]
    public void InvalidAttributeConfigurationError_CarriesOnlyTheTag() =>
        // The parser never reports this error for any attribute layout we could build; its constructor is internal.
        Check(new ErrorData((InvalidAttributeConfigurationError)Activator.CreateInstance(
                typeof(InvalidAttributeConfigurationError),
                true)!),
            ErrorType.InvalidAttributeConfigurationError);

    [Fact]
    public void MissingGroupOptionError_CarriesOnlyTheTag() =>
        Check(new ErrorData(Single<MissingGroupOptionError>(typeof(CliFixtures.Grouped))), ErrorType.MissingGroupOptionError);

    [Fact]
    public void GroupOptionAmbiguityError_KeepsTheNameInfo() =>
        Check(new ErrorData(Single<GroupOptionAmbiguityError>(typeof(CliFixtures.GroupAndSet), "--gg", "1")),
            ErrorType.GroupOptionAmbiguityError,
            nameInfoString: "|gg|gg");

    [Fact]
    public void MultipleDefaultVerbsError_CarriesOnlyTheTag() =>
        Check(new ErrorData(Single<MultipleDefaultVerbsError>(typeof(CliFixtures.DefaultVerbA), typeof(CliFixtures.DefaultVerbB))),
            ErrorType.MultipleDefaultVerbsError);

    private static IReadOnlyList<Error> Errors(Type type, params string[] args) =>
        CliFixtures.ErrorsOfOptions(type, args);

    private static TError Single<TError>(Type type, params string[] args) where TError : Error =>
        CliFixtures.ErrorsOfOptions(type, args).OfType<TError>().Single();

    private static TError Single<TError>(Type first, Type second, params string[] args) where TError : Error =>
        CliFixtures.ErrorsOf([first, second], args).OfType<TError>().Single();

    private static void Check(ErrorData data,
                              ErrorType tag,
                              string? nameInfoString = null,
                              string? token = null,
                              string? setName = null,
                              string? verb = null,
                              Exception? exception = null)
    {
        data.Tag.ShouldBe(tag);
        data.NameInfoString.ShouldBe(nameInfoString);
        data.Token.ShouldBe(token);
        data.SetName.ShouldBe(setName);
        data.Verb.ShouldBe(verb);
        data.Exception.ShouldBeSameAs(exception);
    }
}
