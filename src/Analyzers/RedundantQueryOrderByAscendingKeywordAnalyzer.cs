using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0015: <c>ascending</c> in a query <c>orderby</c>, which is the default direction.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantQueryOrderByAscendingKeywordAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0015";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant 'ascending' in a query orderby",
        "The 'ascending' keyword is redundant: orderby sorts ascending by default",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "An orderby ordering sorts ascending unless it says descending. Replaces the ReSharper inspection RedundantQueryOrderByAscendingKeyword.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.AscendingOrdering);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var keyword = ((OrderingSyntax)context.Node).AscendingOrDescendingKeyword;
        if (keyword.IsKind(SyntaxKind.AscendingKeyword))
            context.ReportDiagnostic(Diagnostic.Create(Rule, keyword.GetLocation()));
    }
}
