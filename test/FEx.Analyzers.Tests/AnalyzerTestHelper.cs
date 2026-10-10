using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace FEx.Analyzers.Tests;

internal static class AnalyzerTestHelper
{
    /// <summary>
    /// Runs <typeparamref name="TAnalyzer"/> over <paramref name="source"/>, which must compile.
    /// Mark each expected hit as <c>{|FEX0001:code|}</c>; unmarked code must stay silent.
    /// </summary>
    public static async Task VerifyAsync<TAnalyzer>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };
        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var options = (CSharpCompilationOptions)solution.GetProject(projectId)!.CompilationOptions!;

            return solution.WithProjectCompilationOptions(projectId, options.WithAllowUnsafe(true));
        });

        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
