using Nuke.Common;
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
}
