using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// Redaction by parameter identity rather than by the one value NUKE resolved. Both issues this pins were
/// measured on a real run: a repeated <c>--nuget-api-key</c> printed every value but the last (#55), and a
/// <c>NuGetSource</c> carrying <c>user:TOKEN@</c> from an environment variable was printed in full by the
/// parameter listing (#57).
/// </summary>
[Collection(GlobalLoggerCollection.Name)]
public sealed class LogRedactionTests
{
    private const string FirstKey = "LEAKFIRST111";
    private const string LastKey = "SECONDKEY222";
    private const string FeedToken = "ghp_ENVPAT777";
    private const string CredentialFeed = $"https://user:{FeedToken}@nuget.pkg.github.com/org/index.json";

    [Theory]
    [InlineData("_build.dll Clean --nuget-api-key {0} --nuget-api-key {1}")]
    [InlineData("_build.dll Clean --nuget-api-key={0} --nuget-api-key={1}")]
    [InlineData("_build.dll Clean -NuGetApiKey {0} --nugetapikey {1}")]
    [InlineData("_build.dll Clean --NUGET-API-KEY \"{0}\" --nuget-api-key {1} --configuration Release")]
    public void ARepeatedSecretOption_NeverShowsTheEarlierValue(string commandLine)
    {
        // NUKE binds the LAST occurrence, so the build only ever knows LastKey - FirstKey is in no value list.
        var build = new ApiKeyBuild(LastKey);

        var echo = build.Redactor.Redact(string.Format(commandLine, FirstKey, LastKey));

        echo.ShouldNotContain(FirstKey);
        echo.ShouldNotContain(LastKey);
        echo.ShouldStartWith("_build.dll Clean ");
    }

    [Fact]
    public void ASecretOption_KeepsTheRestOfTheLineReadable()
    {
        var echo = new ApiKeyBuild(LastKey).Redactor.Redact(
            $"_build.dll Publish --nuget-api-key {FirstKey} --configuration Release");

        echo.ShouldBe($"_build.dll Publish --nuget-api-key {SecretRedactor.Mask} --configuration Release");
    }

    [Fact]
    public void AnOptionThatMerelyStartsWithASecretName_IsLeftAlone()
    {
        var echo = new ApiKeyBuild(LastKey).Redactor.Redact("_build.dll --nuget-api-key-file keys.txt");

        echo.ShouldBe("_build.dll --nuget-api-key-file keys.txt");
    }

    [Fact]
    public void ASecretWhoseGetterThrows_IsStillRedactedByName()
    {
        // The value never reaches the redactor, so only the option name can catch it.
        var echo = new ThrowingKeyBuild().Redactor.Redact($"_build.dll Publish --nuget-api-key {FirstKey}");

        echo.ShouldNotContain(FirstKey);
    }

    [Theory]
    [InlineData(CredentialFeed, FeedToken)]
    [InlineData("https://pkgs.dev.azure.com/org/_packaging/f/nuget/v3/index.json?api-key=AZTOKEN99", "AZTOKEN99")]
    [InlineData("https://feed.example/index.json?sig=SIG123&sv=2020", "SIG123")]
    public void AUrlCredential_IsRedactedWhereverItAppears(string url, string credential)
    {
        var line = new SecretRedactor([], []).Redact($"Pushing 3 package(s) to {url}");

        line.ShouldNotContain(credential);
        line.ShouldContain(SecretRedactor.Mask);
    }

    [Fact]
    public void APlainUrl_IsLeftAlone()
    {
        new SecretRedactor([], []).Redact("Pushing to https://api.nuget.org/v3/index.json?semVerLevel=2.0.0")
            .ShouldBe("Pushing to https://api.nuget.org/v3/index.json?semVerLevel=2.0.0");
    }

    /// <summary>
    /// The environment-variable route of #57: the command line shows nothing, and the listing printed the
    /// value. Drives LogBuildInfo itself - the listing is emitted before the build plan, which needs a running
    /// build and throws here.
    /// </summary>
    [Fact]
    public void ANuGetSourceFromAnEnvironmentVariable_IsRedactedInTheParameterListing()
    {
        var previousSource = Environment.GetEnvironmentVariable("NuGetSource");
        Environment.SetEnvironmentVariable("NuGetSource", CredentialFeed);

        try
        {
            var build = new EnvironmentSourceBuild();

            // Proves the value genuinely came from the environment, so the listing has it to print.
            ((INuGetPublishTarget)build).NuGetSource.ShouldBe(CredentialFeed);

            var messages = Capture(build.PrintBuildInfo);

            var listing = messages.Single(static m => m.Contains("NuGetSource:"));
            listing.ShouldNotContain(FeedToken);
            listing.ShouldContain("nuget.pkg.github.com");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NuGetSource", previousSource);
        }
    }

    /// <summary>
    /// The central point: once the build installs it, a line logged anywhere - a target, NUKE itself - is
    /// redacted, in the template text as well as in its properties.
    /// </summary>
    [Fact]
    public void TheInstalledPipeline_RedactsEveryLineWrittenThroughTheGlobalLogger()
    {
        var build = new ApiKeyBuild(LastKey);

        var messages = Capture(() =>
        {
            build.Install();
            Log.Information("Pushing {Count} package(s) to {Source}", 2, CredentialFeed);
            Log.Information($"> dotnet nuget push --api-key {FirstKey} --source {CredentialFeed}");
            Log.Information("key {Key}", LastKey);
        });

        var all = string.Join(Environment.NewLine, messages);

        messages.Count.ShouldBe(3);
        all.ShouldNotContain(FeedToken);
        all.ShouldNotContain(LastKey);
        all.ShouldContain("Pushing 2 package(s)");
    }

    private static List<string> Capture(Action action)
    {
        var sink = new CapturingSink();
        var previous = Log.Logger;

        Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(sink).CreateLogger();

        try
        {
            action();
        }
        catch (Exception)
        {
            // LogBuildInfo throws once it reaches NUKE's execution plan; the lines before it are emitted.
        }
        finally
        {
            Log.Logger = previous;
        }

        return sink.Messages;
    }

    private sealed class ApiKeyBuild(string key) : FExBuild, INuGetPublishTarget
    {
        public override IEnumerable<string> PublishProjects { get; } = [];

        public new SecretRedactor Redactor => base.Redactor;

        string? INuGetPublishTarget.NuGetApiKey => key;

        public void Install() => InstallLogRedaction();
    }

    private sealed class ThrowingKeyBuild : FExBuild, INuGetPublishTarget
    {
        public override IEnumerable<string> PublishProjects { get; } = [];

        public new SecretRedactor Redactor => base.Redactor;

        string? INuGetPublishTarget.NuGetApiKey => throw new InvalidOperationException("key rejected");
    }

    // Keeps INuGetPublishTarget's own NuGetSource getter, so the value is read the way a real build reads it.
    private sealed class EnvironmentSourceBuild : FExBuild, INuGetPublishTarget
    {
        public override IEnumerable<string> PublishProjects { get; } = [];

        public void PrintBuildInfo() => LogBuildInfo();
    }

    private sealed class CapturingSink : ILogEventSink
    {
        public List<string> Messages { get; } = [];

        public void Emit(LogEvent logEvent) => Messages.Add(logEvent.RenderMessage());
    }
}
