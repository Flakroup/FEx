using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Threading;

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
        "'x is string { }' tests exactly what 'x is string' does. Replaces the ReSharper inspection RedundantPropertyPatternClause.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

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
        if (pattern.PropertyPatternClause is not { Subpatterns.Count: 0 } clause
            || pattern.Type is null && pattern.PositionalPatternClause is null)
        {
            return;
        }

        // 'int { }' becomes the type pattern 'int', which means more than the clause did in two places.
        if (pattern is { Type: not null, PositionalPatternClause: null, Designation: null }
            && (!IsTypePatternAvailable(pattern) || IsAlwaysTrueTypeTest(pattern, context.SemanticModel, context.CancellationToken)))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, clause.GetLocation()));
    }

    // A type pattern outside 'x is T' is C# 9; 'case string { }:' would become 'case string:', which older versions read as a constant.
    private static bool IsTypePatternAvailable(RecursivePatternSyntax pattern) =>
        pattern.Parent is IsPatternExpressionSyntax
        || pattern.SyntaxTree.Options is not CSharpParseOptions { LanguageVersion: < LanguageVersion.CSharp9 };

    // 'i is int { }' on an int is fine, 'i is int' is the warning CS0183 (an error under warnings-as-errors).
    private static bool IsAlwaysTrueTypeTest(RecursivePatternSyntax pattern, SemanticModel model, CancellationToken cancellationToken)
    {
        SyntaxNode node = pattern;
        while (node.Parent is UnaryPatternSyntax or ParenthesizedPatternSyntax or BinaryPatternSyntax)
            node = node.Parent;

        return node.Parent is IsPatternExpressionSyntax test
            && model.GetTypeInfo(test.Expression, cancellationToken).Type is { IsValueType: true } input
            && input.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T;
    }
}
