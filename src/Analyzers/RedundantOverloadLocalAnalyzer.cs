using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0040: a private method or constructor that only forwards to a sibling, supplying the sibling's own defaults.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantOverloadLocalAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0040";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant overload (Local)",
        "Overload '{0}' only forwards to a sibling with its default arguments; the optional parameters already cover it",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "Callers of the sibling get the same call without the extra overload, which only adds a second candidate to overload resolution. Replaces the ReSharper inspection RedundantOverload.Local.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration, SyntaxKind.ConstructorDeclaration);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (RedundantOverloadAnalysis.FindRedundant(context) is { } method && AnalyzerHelpers.IsEffectivelyPrivate(method))
            context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations[0], method.Name == ".ctor" ? method.ContainingType.Name : method.Name));
    }
}
