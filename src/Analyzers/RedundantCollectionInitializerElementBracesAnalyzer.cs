using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0014: <c>{ { x } }</c> in a collection initializer, where <c>{ x }</c> adds the same element.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantCollectionInitializerElementBracesAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0014";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant braces around a collection initializer element",
        "The braces around this single collection initializer element are redundant",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "An element written as '{ x }' calls Add(x) exactly as 'x' does. Braces around an assignment or a collection expression are kept, since without them the element would mean something else. Replaces the ReSharper inspection RedundantCollectionInitializerElementBraces.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ComplexElementInitializerExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var element = (InitializerExpressionSyntax)context.Node;
        if (element.Expressions.Count != 1)
            return;

        // Unbraced, a bare 'x = 1' would be a member initializer and '[1, 2]' an indexer initializer.
        var only = element.Expressions[0];
        if (!only.IsKind(SyntaxKind.SimpleAssignmentExpression) && !only.IsKind(SyntaxKind.CollectionExpression))
            context.ReportDiagnostic(Diagnostic.Create(Rule, element.OpenBraceToken.GetLocation()));
    }
}
