using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0031: an empty <c>{ }</c> after a type or a positional pattern, which adds nothing to what the pattern already tests.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantPropertyPatternClauseAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0031";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant property pattern clause",
        "The empty property pattern clause is redundant: the pattern tests the same without it",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "'x is string { }' tests exactly what 'x is string' does. Replaces the ReSharper inspection RedundantPropertyPatternClause.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.RecursivePattern);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var pattern = (RecursivePatternSyntax)context.Node;

        // A bare '{ }' is the null check; it only goes redundant behind a type or a positional clause.
        if (pattern.PropertyPatternClause is { Subpatterns.Count: 0 } clause
            && (pattern.Type is not null || pattern.PositionalPatternClause is not null))
            context.ReportDiagnostic(Diagnostic.Create(Rule, clause.GetLocation()));
    }
}
