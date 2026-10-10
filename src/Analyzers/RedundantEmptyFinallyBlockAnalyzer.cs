using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0016: a <c>finally</c> block with no statement.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantEmptyFinallyBlockAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0016";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant empty finally block",
        "This empty 'finally' block is redundant",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "A finally block that runs no statement does nothing. Replaces the ReSharper inspection RedundantEmptyFinallyBlock.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.FinallyClause);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var clause = (FinallyClauseSyntax)context.Node;
        if (clause.Block.Statements.Count == 0)
            context.ReportDiagnostic(Diagnostic.Create(Rule, clause.GetLocation()));
    }
}
