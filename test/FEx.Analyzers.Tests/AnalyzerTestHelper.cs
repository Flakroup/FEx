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
    /// Pass <see cref="CompilerDiagnostics.None"/> to run over code that does not compile, the way an editor does while the user types.
    /// <paramref name="editorConfig"/> is the text of the <c>.editorconfig</c> at the root of the project.
    /// <paramref name="references"/> are assemblies the source may use besides the framework: code in a library is not code in source.
    /// </summary>
    public static async Task VerifyAsync<TAnalyzer>(
        string source,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        LanguageVersion? languageVersion = null,
        string[]? otherFiles = null,
        DiagnosticResult[]? expected = null,
        CompilerDiagnostics compilerDiagnostics = CompilerDiagnostics.Errors,
        string? editorConfig = null,
        MetadataReference[]? references = null)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = Net10,
            CompilerDiagnostics = compilerDiagnostics,
        };
        foreach (var (text, index) in (otherFiles ?? []).Select((text, index) => (text, index)))
            test.TestState.Sources.Add(($"Other{index}.cs", text));
        foreach (var reference in references ?? [])
            test.TestState.AdditionalReferences.Add(reference);
        if (editorConfig is not null)
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
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
