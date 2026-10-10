using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0032: an operand of <c>&amp;&amp;</c> that is always true, or of <c>||</c> that is always false.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantLogicalConditionalExpressionOperandAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0032";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant logical conditional expression operand",
        "The operand is redundant: it never changes the result of the logical operator",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "'a && true' and 'a || false' are 'a'. Replaces the ReSharper inspection RedundantLogicalConditionalExpressionOperand.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.LogicalAndExpression, SyntaxKind.LogicalOrExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var binary = (BinaryExpressionSyntax)context.Node;

        // The value that changes nothing: true for 'and', false for 'or'.
        var neutral = binary.IsKind(SyntaxKind.LogicalAndExpression);
        foreach (var operand in new[] { binary.Left, binary.Right })
        {
            if (IsConstant(operand, context.SemanticModel, neutral))
                context.ReportDiagnostic(Diagnostic.Create(Rule, operand.GetLocation()));
        }
    }

    private static bool IsConstant(ExpressionSyntax operand, SemanticModel model, bool expected)
    {
        if (model.GetConstantValue(operand) is not { HasValue: true, Value: bool value } || value != expected)
            return false;

        // Naming a constant is a statement of intent, and ReSharper leaves it alone.
        var inner = operand;
        while (inner is ParenthesizedExpressionSyntax parenthesized)
            inner = parenthesized.Expression;

        return inner is not (IdentifierNameSyntax or MemberAccessExpressionSyntax);
    }
}
