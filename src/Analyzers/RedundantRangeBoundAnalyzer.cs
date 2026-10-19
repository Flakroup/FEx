using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

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
        "A range starts at 0 and ends at ^0 when it says nothing, so '0..x' means '..x' and 'x..^0' means 'x..'. A constant that folds to 0 counts, and so does new Index(0). Replaces the ReSharper inspection RedundantRangeBound.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

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
        if (range.LeftOperand is { } start && IsStartDefault(context.SemanticModel, start))
            context.ReportDiagnostic(Diagnostic.Create(Rule, start.GetLocation()));
        if (range.RightOperand is { } end && IsEndDefault(context.SemanticModel, end))
            context.ReportDiagnostic(Diagnostic.Create(Rule, end.GetLocation()));
    }

    // A constant that folds to 0 counts, written as 0, a const, 1 - 1 or (Index)0, and so does an explicit new Index(0) or new Index(0, false).
    private static bool IsStartDefault(SemanticModel model, ExpressionSyntax start) =>
        Strip(start) is var inner && (IsZero(model, inner) || inner is ObjectCreationExpressionSyntax creation && IsIndexFromStartZero(model, creation));

    private static bool IsEndDefault(SemanticModel model, ExpressionSyntax end) =>
        end is PrefixUnaryExpressionSyntax prefix && prefix.IsKind(SyntaxKind.IndexExpression) && IsZero(model, Strip(prefix.Operand));

    // ponytail: only an int-typed constant counts, which is what a range bound is written as; upgrade by widening the pattern.
    private static bool IsZero(SemanticModel model, ExpressionSyntax expression) =>
        model.GetConstantValue(expression).Value is 0;

    private static bool IsIndexFromStartZero(SemanticModel model, ObjectCreationExpressionSyntax creation) =>
        creation.ArgumentList is { Arguments: { Count: 1 or 2 } arguments }
        && IsZero(model, arguments[0].Expression)
        && (arguments.Count == 1 || model.GetConstantValue(arguments[1].Expression).Value is false)
        && model.GetTypeInfo(creation).Type is { Name: "Index", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } };

    private static ExpressionSyntax Strip(ExpressionSyntax expression)
    {
        var current = expression;
        while (true)
        {
            switch (current)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    current = parenthesized.Expression;
                    break;
                case CastExpressionSyntax cast:
                    current = cast.Expression;
                    break;
                default:
                    return current;
            }
        }
    }
}
