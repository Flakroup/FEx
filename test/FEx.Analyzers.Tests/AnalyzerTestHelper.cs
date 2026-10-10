using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace FEx.Analyzers.Tests;

internal static class AnalyzerTestHelper
{
    // Stable net10 reference assemblies, on the targeting pack version the SDK of global.json ships.
    private static readonly ReferenceAssemblies Net10 = new(
        "net10.0",
        new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.12"),
        Path.Combine("ref", "net10.0"));

    /// <summary>
    /// Runs <typeparamref name="TAnalyzer"/> over <paramref name="source"/>, which must compile.
    /// Mark each expected hit as <c>{|FEX0001:code|}</c>; unmarked code must stay silent.
    /// </summary>
    public static async Task VerifyAsync<TAnalyzer>(
        string source,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        LanguageVersion? languageVersion = null,
        string[]? otherFiles = null,
        DiagnosticResult[]? expected = null)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = Net10,
        };
        foreach (var (text, index) in (otherFiles ?? []).Select((text, index) => (text, index)))
            test.TestState.Sources.Add(($"Other{index}.cs", text));
        test.ExpectedDiagnostics.AddRange(expected ?? []);
        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId)!;
            var options = ((CSharpCompilationOptions)project.CompilationOptions!).WithAllowUnsafe(true).WithOutputKind(outputKind);
            var parseOptions = (CSharpParseOptions)project.ParseOptions!;
            if (languageVersion is { } version)
                parseOptions = parseOptions.WithLanguageVersion(version);

            return solution.WithProjectCompilationOptions(projectId, options).WithProjectParseOptions(projectId, parseOptions);
        });

        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
