using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Shouldly;
using Xunit;

namespace FEx.Analyzers.Tests;

/// <summary>
/// The specification of every rule: the ReSharper golden corpus from DevConfigs (<c>analyzers-corpus</c>, recorded on
/// ReSharper 2026.2.3.1). Each analyzer must fire on exactly the samples ReSharper reported its inspection on.
/// </summary>
public class GoldenCorpusTests
{
    private const string SampleMarker = "//# ";

    private static readonly string[] SourceFiles = ["StyleA.cs", "StyleB.cs", "Stubs.cs"];

    private static readonly string[] CorpusLibraries = ["netstandard", "mscorlib", "Serilog", "JetBrains.Annotations"];

    private static readonly Dictionary<string, string> InspectionById = new()
    {
        [NonAtomicCompoundOperatorAnalyzer.DiagnosticId] = "NonAtomicCompoundOperator",
        [LoopVariableNeverChangedAnalyzer.DiagnosticId] = "LoopVariableIsNeverChangedInsideLoop",
        [VariableHidesOuterVariableAnalyzer.DiagnosticId] = "VariableHidesOuterVariable",
        [MemberHidesStaticFromOuterClassAnalyzer.DiagnosticId] = "MemberHidesStaticFromOuterClass",
        [BaseMemberHasParamsAnalyzer.DiagnosticId] = "BaseMemberHasParams",
    };

    [Fact]
    public async Task Each_analyzer_fires_on_exactly_the_samples_ReSharper_reported_its_inspection_on()
    {
        var sources = SourceFiles.ToDictionary(file => file, ReadResource);
        var compilation = Compile(sources);
        compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("the corpus has to compile for the analyzers to see real symbols");

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
            new NonAtomicCompoundOperatorAnalyzer(),
            new LoopVariableNeverChangedAnalyzer(),
            new VariableHidesOuterVariableAnalyzer(),
            new MemberHidesStaticFromOuterClassAnalyzer(),
            new BaseMemberHasParamsAnalyzer());
        var diagnostics = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        // A crashing analyzer reports AD0001 and has no SourceTree to map to a sample; name the crash instead.
        diagnostics.Where(diagnostic => diagnostic.Id == "AD0001").Select(diagnostic => diagnostic.GetMessage()).ShouldBeEmpty();

        var actual = diagnostics
            .Select(diagnostic => $"{diagnostic.Id} {SampleOf(sources, diagnostic)}")
            .Distinct()
            .Order()
            .ToList();
        var expected = ExpectedSamples()
            .Order()
            .ToList();

        expected.ShouldNotBeEmpty();
        actual.ShouldBe(expected);
    }

    private static List<string> ExpectedSamples()
    {
        using var document = JsonDocument.Parse(ReadResource("expected.json"));
        var inspections = document.RootElement.GetProperty("inspections");

        return InspectionById
            .SelectMany(rule => inspections.GetProperty(rule.Value).Deserialize<string[]>()!.Select(sample => $"{rule.Key} {sample}"))
            .ToList();
    }

    // The sample a diagnostic belongs to: the nearest "//# name" line above it, spelled as expected.json spells it.
    private static string SampleOf(Dictionary<string, string> sources, Diagnostic diagnostic)
    {
        var file = Path.GetFileName(diagnostic.Location.SourceTree!.FilePath);
        var lines = sources[file].Split('\n');
        var line = diagnostic.Location.GetLineSpan().StartLinePosition.Line;
        var marker = lines.Take(line + 1).Last(text => text.StartsWith(SampleMarker, StringComparison.Ordinal));

        return $"{file}: {marker.Substring(SampleMarker.Length).TrimEnd()}";
    }

    private static CSharpCompilation Compile(Dictionary<string, string> sources)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var trees = sources
            .Where(source => source.Key.EndsWith(".cs", StringComparison.Ordinal))
            .Select(source => CSharpSyntaxTree.ParseText(source.Value, parseOptions, source.Key))
            .ToList();

        // The runtime's own assemblies stand in for reference assemblies, so the test needs no download.
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => IsCorpusReference(Path.GetFileNameWithoutExtension(path)))
            .Select(path => MetadataReference.CreateFromFile(path));
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable);

        return CSharpCompilation.Create("Corpus", trees, references, options);
    }

    private static bool IsCorpusReference(string assemblyName) =>
        assemblyName.StartsWith("System.", StringComparison.Ordinal) || CorpusLibraries.Contains(assemblyName);

    private static string ReadResource(string name)
    {
        var assembly = typeof(GoldenCorpusTests).Assembly;
        using var stream = assembly.GetManifestResourceStream($"FEx.Analyzers.Tests.CorpusData.{name}")!;
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
