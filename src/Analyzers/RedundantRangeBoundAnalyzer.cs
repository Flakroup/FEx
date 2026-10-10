using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0021: the bound of a range that restates its default: <c>0..</c> or <c>..^0</c>.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantRangeBoundAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0021";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant range bound",
        "This range bound is redundant: it is the default",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "A range starts at 0 and ends at ^0 when it says nothing, so '0..x' means '..x' and 'x..^0' means 'x..'. Replaces the ReSharper inspection RedundantRangeBound.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.RangeExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var range = (RangeExpressionSyntax)context.Node;
        if (range.LeftOperand is { } start && IsStartDefault(Unwrap(start)))
            context.ReportDiagnostic(Diagnostic.Create(Rule, start.GetLocation()));
        if (range.RightOperand is { } end && IsEndDefault(end))
            context.ReportDiagnostic(Diagnostic.Create(Rule, end.GetLocation()));
    }

    private static bool IsStartDefault(ExpressionSyntax start) =>
        IsZero(start) || start is CastExpressionSyntax cast && IsZero(Unwrap(cast.Expression));

    private static bool IsEndDefault(ExpressionSyntax end) =>
        end is PrefixUnaryExpressionSyntax prefix && prefix.IsKind(SyntaxKind.IndexExpression) && IsZero(Unwrap(prefix.Operand));

    private static bool IsZero(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax { Token.Value: 0 };

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        return expression;
    }
}
