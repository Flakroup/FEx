using Shouldly;
using System;
using Xunit;

namespace FEx.CLI.Tests;

/// <summary>GetConfiguration turns arguments into an options object, or throws one exception listing every problem.</summary>
public sealed class CommandLineParserHelperTests
{
    [Fact]
    public void GetConfiguration_ParsesLongAndShortOptions()
    {
        var config = CommandLineParserHelper.GetConfiguration<CliFixtures.Simple>(["--name", "demo", "-p", "8080", "-v"]);

        config.Name.ShouldBe("demo");
        config.Port.ShouldBe(8080);
        config.Verbose.ShouldBeTrue();
    }

    [Fact]
    public void GetConfiguration_AppliesDefaults()
    {
        var config = CommandLineParserHelper.GetConfiguration<CliFixtures.Simple>(["-n", "demo"]);

        config.Port.ShouldBe(80);
        config.Verbose.ShouldBeFalse();
    }

    [Fact]
    public void GetConfiguration_WithNoArgumentsForAnAllOptionalType_ReturnsDefaults()
    {
        var config = CommandLineParserHelper.GetConfiguration<CliFixtures.Optional>([]);

        config.Name.ShouldBeNull();
        config.Port.ShouldBe(0);
    }

    [Fact]
    public void GetConfiguration_ParsesSequences()
    {
        var config = CommandLineParserHelper.GetConfiguration<CliFixtures.Optional>(["--tag", "a", "b", "--pair", "x", "y"]);

        config.Tags.ShouldBe(["a", "b"]);
        config.Pair.ShouldBe(["x", "y"]);
    }

    [Fact]
    public void GetConfiguration_MissingRequiredOption_ThrowsNamingTheOption()
    {
        var ex = Should.Throw<Exception>(() => CommandLineParserHelper.GetConfiguration<CliFixtures.Simple>([]));

        ex.Message.ShouldBe("MissingRequiredOptionError n|name|n, name");
    }

    [Fact]
    public void GetConfiguration_UnknownOption_ThrowsWithTheToken()
    {
        var ex = Should.Throw<Exception>(() =>
            CommandLineParserHelper.GetConfiguration<CliFixtures.Simple>(["-n", "x", "--zzz"]));

        ex.Message.ShouldBe("UnknownOptionError");
    }

    [Fact]
    public void GetConfiguration_BadFormatConversion_ThrowsNamingTheOption()
    {
        var ex = Should.Throw<Exception>(() =>
            CommandLineParserHelper.GetConfiguration<CliFixtures.Simple>(["-n", "x", "--port", "abc"]));

        ex.Message.ShouldBe("BadFormatConversionError p|port|p, port");
    }

    [Fact]
    public void GetConfiguration_ManyProblems_AreJoinedIntoOneMessage()
    {
        var ex = Should.Throw<Exception>(() => CommandLineParserHelper.GetConfiguration<CliFixtures.Simple>(["--port", "abc"]));

        ex.Message.ShouldContain("MissingRequiredOptionError n|name|n, name");
        ex.Message.ShouldContain("BadFormatConversionError p|port|p, port");
        ex.Message.ShouldContain(", ");
    }

    [Fact]
    public void GetConfiguration_MissingSequenceValue_ThrowsNamingTheOption()
    {
        var ex = Should.Throw<Exception>(() => CommandLineParserHelper.GetConfiguration<CliFixtures.Optional>(["--tag"]));

        ex.Message.ShouldBe("MissingValueOptionError |tag|tag");
    }

    [Fact]
    public void GetConfiguration_SequenceOutOfRange_ThrowsNamingTheOption()
    {
        var ex = Should.Throw<Exception>(() =>
            CommandLineParserHelper.GetConfiguration<CliFixtures.Optional>(["--pair", "only-one"]));

        ex.Message.ShouldBe("SequenceOutOfRangeError |pair|pair");
    }

    [Fact]
    public void GetConfiguration_RepeatedOption_ThrowsNamingTheOption()
    {
        var ex = Should.Throw<Exception>(() =>
            CommandLineParserHelper.GetConfiguration<CliFixtures.Optional>(["--port", "1", "--port", "2"]));

        ex.Message.ShouldBe("RepeatedOptionError p|port|p, port");
    }

    [Fact]
    public void GetConfiguration_BadFormatToken_Throws()
    {
        var ex = Should.Throw<Exception>(() => CommandLineParserHelper.GetConfiguration<CliFixtures.Optional>(["--name="]));

        ex.Message.ShouldBe("BadFormatTokenError");
    }

    [Fact]
    public void GetConfiguration_MutuallyExclusiveSets_ThrowsNamingTheSet()
    {
        var ex = Should.Throw<Exception>(() =>
            CommandLineParserHelper.GetConfiguration<CliFixtures.Exclusive>(["--aa", "1", "--bb", "2"]));

        ex.Message.ShouldContain("MutuallyExclusiveSetError set-");
    }

    [Fact]
    public void GetConfiguration_MissingGroupOption_Throws()
    {
        var ex = Should.Throw<Exception>(() => CommandLineParserHelper.GetConfiguration<CliFixtures.Grouped>([]));

        ex.Message.ShouldBe("MissingGroupOptionError");
    }

    [Fact]
    public void GetConfiguration_GroupAndSetTogether_Throws()
    {
        var ex = Should.Throw<Exception>(() =>
            CommandLineParserHelper.GetConfiguration<CliFixtures.GroupAndSet>(["--gg", "1"]));

        ex.Message.ShouldBe("GroupOptionAmbiguityError |gg|gg");
    }

    [Fact]
    public void GetConfiguration_ASetterThatThrows_ReportsTheException()
    {
        var ex = Should.Throw<Exception>(() =>
            CommandLineParserHelper.GetConfiguration<CliFixtures.Throwing>(["--boom", "x"]));

        ex.Message.ShouldContain("SetValueExceptionError |boom|boom");
        ex.Message.ShouldContain("setter exploded");
    }

    [Fact]
    public void GetConfiguration_HelpRequest_ThrowsAHelpRequestedError()
    {
        var ex = Should.Throw<Exception>(() => CommandLineParserHelper.GetConfiguration<CliFixtures.Simple>(["--help"]));

        ex.Message.ShouldBe("HelpRequestedError");
    }

    [Fact]
    public void GetConfiguration_VersionRequest_ThrowsAVersionRequestedError()
    {
        var ex = Should.Throw<Exception>(() => CommandLineParserHelper.GetConfiguration<CliFixtures.Simple>(["--version"]));

        ex.Message.ShouldBe("VersionRequestedError");
    }
}
