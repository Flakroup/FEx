using Nuke.Common;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The build logs every [Parameter] it can reach, and the NuGet API key is one of them. Measured before
/// this was fixed: a run carrying <c>--nuget-api-key</c> printed the key in full, twice - once in the
/// command line echo and once in the parameter listing. GitHub Actions masks values that came from
/// <c>secrets.*</c>, but nothing masks a local run, a binary log, or another CI.
/// </summary>
public sealed class SecretMaskingTests
{
    [Fact]
    public void ASecretsValueIsNeverPrintedInTheCommandLineEcho()
    {
        var build = new SecretCarryingBuild();

        var masked = build.Mask($"_build.dll Publish --nuget-api-key {SecretCarryingBuild.Key} --configuration Release");

        masked.ShouldNotContain(SecretCarryingBuild.Key);
        masked.ShouldContain(FExBuild.SecretMask);
    }

    [Theory]
    [InlineData("--nuget-api-key {0}")]
    [InlineData("--nuget-api-key={0}")]
    [InlineData("/nuget-api-key:{0}")]
    public void TheMaskDoesNotDependOnHowTheSecretWasPassed(string form)
    {
        // Matched by value, not by option name - so it holds for an environment variable or a parameters
        // file too, and does not rely on reproducing NUKE's option-name casing.
        var build = new SecretCarryingBuild();

        build.Mask(string.Format(form, SecretCarryingBuild.Key)).ShouldNotContain(SecretCarryingBuild.Key);
    }

    [Fact]
    public void AValueAppearingSeveralTimesIsMaskedEverywhere()
    {
        var build = new SecretCarryingBuild();

        var masked = build.Mask($"{SecretCarryingBuild.Key} middle {SecretCarryingBuild.Key}");

        masked.ShouldBe($"{FExBuild.SecretMask} middle {FExBuild.SecretMask}");
    }

    [Fact]
    public void TextWithoutASecretIsLeftAlone()
    {
        new SecretCarryingBuild().Mask("_build.dll Compile --configuration Release")
            .ShouldBe("_build.dll Compile --configuration Release");
    }

    [Fact]
    public void AnUnsetSecretMasksNothing()
    {
        // An empty secret would otherwise match everywhere and mask the whole line.
        new EmptySecretBuild().Mask("_build.dll Compile").ShouldBe("_build.dll Compile");
    }

    /// <summary>
    /// The listing is built by reflection over every [Parameter], so a secret reaches it by default. This
    /// pins that the masking sits on the collecting side - before <see cref="FExBuild.FormatParameterValue" />,
    /// which is overridable and would otherwise be handed the real value.
    /// </summary>
    [Fact]
    public void MaskingHappensBeforeTheOverridableFormatter()
    {
        var build = new SecretCarryingBuild();

        build.LogAndCapture();

        build.FormattedValues.ShouldNotContain(SecretCarryingBuild.Key);
        build.FormattedValues.ShouldContain(FExBuild.SecretMask);
    }

    [Fact]
    public void EverySecretParameterInThisAssemblyIsCoveredByTheMask()
    {
        // A new [Secret] parameter added later is covered automatically - unless someone drops the
        // attribute, which is the case this asserts against.
        typeof(INuGetPublishTarget)
            .GetProperty(nameof(INuGetPublishTarget.NuGetApiKey),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .ShouldNotBeNull()
            .GetCustomAttribute<SecretAttribute>()
            .ShouldNotBeNull();
    }

    /// <summary>
    /// The call site, not just the helper. `ASecretsValueIsNeverPrintedInTheCommandLineEcho` exercises
    /// MaskSecrets directly, so reverting LogBuildInfo's echo to the raw Environment.CommandLine left the
    /// suite fully green while the key went back into the log in full (measured). This drives LogBuildInfo
    /// itself: the echo is its FIRST line, so everything that needs a running build - Solution.Path,
    /// ExecutionPlan - throws afterwards and the assertion still has what it came for.
    /// </summary>
    [Fact]
    public void LogBuildInfoItself_MasksTheEchoItPrints()
    {
        var build = new CommandLineSecretBuild();
        var sink = new CapturingSink();
        var previous = Log.Logger;

        Log.Logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();

        try
        {
            try
            {
                build.PrintBuildInfo();
            }
            catch (Exception)
            {
                // Expected: the listing needs NUKE's execution state. The echo is already emitted.
            }
        }
        finally
        {
            Log.Logger = previous;
        }

        var echo = sink.Messages.Single(static m => m.Contains("Command Line:"));

        echo.ShouldNotContain(CommandLineSecretBuild.Key);
        echo.ShouldContain(FExBuild.SecretMask);
    }

    /// <summary>
    /// A consumer that re-declares a secret parameter on its own build class shadows the interface member
    /// carrying [Secret], and attributes do not cross that boundary. Reflection yields class properties
    /// first, so keying the mask on the property in hand printed the key in full - while the command-line
    /// echo on the same object still showed ***, which reads as proof the masking works.
    /// </summary>
    [Fact]
    public void ASecretRedeclaredOnTheBuildClass_IsStillMasked()
    {
        var build = new ShadowingSecretBuild();

        build.LogAndCapture();

        build.FormattedValues.ShouldNotContain(ShadowingSecretBuild.ShadowedKey);
        build.FormattedValues.ShouldContain(FExBuild.SecretMask);
    }

    private class SecretCarryingBuild : FExBuild, INuGetPublishTarget
    {
        public const string Key = "oy2-FAKE-KEY-DO-NOT-USE";

        public override IEnumerable<string> PublishProjects { get; } = [];

        public List<string> FormattedValues { get; } = [];

        string? INuGetPublishTarget.NuGetApiKey => Key;

        public string Mask(string text) => MaskSecrets(text);

        // Runs the REAL collecting path - the one LogBuildInfo uses - rather than a copy of it, so the
        // test cannot stay green while the production masking is removed. LogBuildInfo itself is out of
        // reach here: it reads NUKE's execution plan, which needs a running build.
        public void LogAndCapture()
        {
            foreach (var (name, value) in GetParameterEntries(new(StringComparer.OrdinalIgnoreCase)))
                FormattedValues.Add(FormatParameterValue(name, value));
        }
    }

    // Re-implements the interface so its own (empty) key wins over the base class's.
    private sealed class EmptySecretBuild : SecretCarryingBuild, INuGetPublishTarget
    {
        string? INuGetPublishTarget.NuGetApiKey => "";
    }

    // The shape a consumer writes when it wants to source the key itself - and the one this repo's own
    // test comments recommend, since NUKE binds command-line parameters on the build class.
    private sealed class ShadowingSecretBuild : FExBuild, INuGetPublishTarget
    {
        public const string ShadowedKey = "shadowed-key-must-not-print";

        public override IEnumerable<string> PublishProjects { get; } = [];

        public List<string> FormattedValues { get; } = [];

        [Parameter("NuGet API key for pushing packages")]
        public string? NuGetApiKey => ShadowedKey;

        public void LogAndCapture()
        {
            foreach (var (name, value) in GetParameterEntries(new(StringComparer.OrdinalIgnoreCase)))
                FormattedValues.Add(FormatParameterValue(name, value));
        }
    }

    // Its key is a slice of the real command line, so the echo genuinely carries it - a fake that does not
    // appear there would produce identical output masked or not, and the test would pass either way.
    private sealed class CommandLineSecretBuild : FExBuild, INuGetPublishTarget
    {
        public static readonly string Key = Environment.CommandLine[..16];

        public override IEnumerable<string> PublishProjects { get; } = [];

        string? INuGetPublishTarget.NuGetApiKey => Key;

        public void PrintBuildInfo() => LogBuildInfo();
    }

    private sealed class CapturingSink : ILogEventSink
    {
        public List<string> Messages { get; } = [];

        public void Emit(LogEvent logEvent) => Messages.Add(logEvent.RenderMessage());
    }
}
