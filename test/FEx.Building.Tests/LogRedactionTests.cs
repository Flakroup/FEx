using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
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

    [Theory]
    [InlineData("https://user:pa'ss@feed.example/index.json", "pa'ss")]
    [InlineData("https://ghp_PATASUSER@github.com/org/repo.git", "ghp_PATASUSER")]
    [InlineData("https://feed.example/index.json?x-api-key=HDRKEY1", "HDRKEY1")]
    [InlineData("https://feed.example/index.json?client_secret=CS1&a=1", "CS1")]
    [InlineData("ssh://git:SSHPW1@host/repo.git", "SSHPW1")]
    public void EveryShapeOfUrlCredential_IsRedacted(string url, string credential) =>
        new SecretRedactor([], []).Redact($"fetching {url} failed").ShouldNotContain(credential);

    [Theory]
    [InlineData("see https://example.com/page?author=bob&design=3&assign=x")]
    [InlineData("cmd /c a&key=1 and tokenizer=bar?auth=1")]
    [InlineData("error: unable to reach ssh://git@github.com/org/repo.git")]
    public void TextThatOnlyLooksLikeACredential_IsLeftReadable(string text) =>
        new SecretRedactor([], []).Redact(text).ShouldBe(text);

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

            var messages = Capture(() =>
            {
                try
                {
                    build.PrintBuildInfo();
                }
                catch (Exception)
                {
                    // LogBuildInfo throws once it reaches NUKE's execution plan; the listing is already emitted.
                }
            });

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

    /// <summary>
    /// The review's measured leak: a failed <c>dotnet nuget push</c> throws a ProcessException whose message
    /// repeats the command line, and NUKE logs it as <c>Log.Error(exception, ...)</c>. Sinks render the exception
    /// through its ToString(), so this renders the event exactly as NUKE's console template does.
    /// </summary>
    [Fact]
    public void AnExceptionsText_IsRedactedAsNukeRendersIt()
    {
        var build = new ApiKeyBuild(LastKey);
        var failure = new InvalidOperationException(
            $"Process 'dotnet' exited with code 1.\n   > dotnet nuget push pkg.nupkg --source {CredentialFeed} --api-key {LastKey}");

        var rendered = Render(() =>
        {
            build.Install();
            Log.Error(failure, "Target {TargetName} has thrown an exception", "Publish");
        });

        rendered.ShouldContain("Target Publish has thrown an exception");
        rendered.ShouldContain("nuget.pkg.github.com");
        rendered.ShouldNotContain(FeedToken);
        rendered.ShouldNotContain(LastKey);
    }

    /// <summary>
    /// The install itself, through the hook NUKE calls - not the helper. Deleting the call from
    /// OnBuildInitialized left every other test green while NUKE's own lines went out unredacted (review, measured).
    /// </summary>
    [Fact]
    public void OnBuildInitialized_InstallsTheRedactingPipeline()
    {
        var build = new ApiKeyBuild(LastKey);

        var messages = Capture(() =>
        {
            try
            {
                build.Initialize();
            }
            catch (Exception)
            {
                // LogBuildInfo throws at NUKE's execution plan; the pipeline is installed before it.
            }

            Log.Information("> dotnet nuget push --source {Source}", CredentialFeed);
        });

        messages.Last().ShouldNotContain(FeedToken);
    }

    /// <summary>
    /// NUKE's errors-and-warnings summary assigns a fresh console logger to Log.Logger before it calls
    /// OnBuildFinished, so whatever an override logs there skipped the wrapper installed at start.
    /// </summary>
    [Fact]
    public void OnBuildFinished_ReinstallsTheRedactionNukeReplaced()
    {
        var build = new ApiKeyBuild(LastKey);

        var messages = Capture(() =>
        {
            build.Install();

            // What Host.WriteErrorsAndWarnings does: a brand-new logger, no redaction.
            var sink = new CapturingSink();
            Log.Logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();

            build.Finish();
            Log.Information("Published to {Source}", CredentialFeed);

            sink.Messages.ShouldHaveSingleItem().ShouldNotContain(FeedToken);
        });

        messages.ShouldBeEmpty();
    }

    [Fact]
    public void InstallingTwice_WrapsTheLoggerOnce()
    {
        var build = new ApiKeyBuild(LastKey);

        Capture(() =>
        {
            build.Install();
            var first = Log.Logger;

            build.Install();

            Log.Logger.ShouldBeSameAs(first);
        });
    }

    [Fact]
    public void DisposingTheWrapper_DisposesTheLoggerItWraps()
    {
        // Log.CloseAndFlush() only reaches the wrapper; without this hand-off NUKE's buffered tail is lost.
        using var sink = new DisposalSink();
#pragma warning disable IDISP001 // Ownership passes to the wrapper; disposing it is what this test checks
        var inner = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
#pragma warning restore IDISP001

        RedactingLogSink.Wrap(inner, new SecretRedactor([], [])).Dispose();

        sink.Disposed.ShouldBeTrue();
    }

    [Fact]
    public void SecretsInsideStructuredProperties_AreRedacted()
    {
        var sink = new CapturingSink();

#pragma warning disable IDISP001 // Ownership passes to the wrapper, which the using below disposes
        var inner = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
#pragma warning restore IDISP001

        using (var logger = RedactingLogSink.Wrap(inner, new SecretRedactor([], [])))
        {
            logger.Information("{Args}", (object)new[] { "push", CredentialFeed });
            logger.Information("{@Feed}", new { Source = CredentialFeed });
            logger.Information("{Feeds}", new Dictionary<string, string> { ["private"] = CredentialFeed });
            logger.Information("{Uri}", new Uri(CredentialFeed));
        }

        sink.Messages.Count.ShouldBe(4);
        sink.Messages.ShouldAllBe(static m => !m.Contains(FeedToken) && m.Contains("nuget.pkg.github.com"));
    }

    private static string Render(Action action)
    {
        using var writer = new StringWriter();
        var sink = new CapturingSink(new MessageTemplateTextFormatter("[{Level:u3}] {Message:l}{NewLine}{Exception}"), writer);
        var previous = Log.Logger;

        Log.Logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();

        try
        {
            action();
        }
        finally
        {
            Log.Logger = previous;
        }

        return writer.ToString();
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

        public void Initialize() => OnBuildInitialized();

        public void Finish() => OnBuildFinished();
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

    private sealed class CapturingSink(MessageTemplateTextFormatter? formatter = null, TextWriter? output = null)
        : ILogEventSink
    {
        public List<string> Messages { get; } = [];

        public void Emit(LogEvent logEvent)
        {
            Messages.Add(logEvent.RenderMessage());

            if (formatter is not null && output is not null)
                formatter.Format(logEvent, output);
        }

    }

    private sealed class DisposalSink : ILogEventSink, IDisposable
    {
        public bool Disposed { get; private set; }

        public void Emit(LogEvent logEvent)
        {
        }

        public void Dispose() => Disposed = true;
    }
}
