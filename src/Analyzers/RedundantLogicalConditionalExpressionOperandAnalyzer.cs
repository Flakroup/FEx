using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

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
        "'a && true' and 'a || false' are 'a'. Replaces the ReSharper inspection RedundantLogicalConditionalExpressionOperand.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

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
        var model = context.SemanticModel;

        // Only plain bool operands, which use the built-in operator: a user-defined '&', '|', 'true' or 'false' and a 'dynamic' operand run code of their own.
        if (model.GetTypeInfo(binary.Left, context.CancellationToken).Type?.SpecialType != SpecialType.System_Boolean
            || model.GetTypeInfo(binary.Right, context.CancellationToken).Type?.SpecialType != SpecialType.System_Boolean)
        {
            return;
        }

        // The value that changes nothing: true for 'and', false for 'or'.
        var neutral = binary.IsKind(SyntaxKind.LogicalAndExpression);
        foreach (var operand in new[] { binary.Left, binary.Right })
        {
            if (IsConstant(operand, model, neutral, context.CancellationToken))
                context.ReportDiagnostic(Diagnostic.Create(Rule, operand.GetLocation()));
        }
    }

    private static bool IsConstant(ExpressionSyntax operand, SemanticModel model, bool expected, CancellationToken cancellationToken)
    {
        if (model.GetConstantValue(operand, cancellationToken) is not { HasValue: true, Value: bool value } || value != expected)
            return false;

        // Naming a constant anywhere in the operand ('Flag', '!Flag', 'Flag == true') is a statement of intent, and ReSharper leaves it alone.
        return !operand.DescendantNodesAndSelf().Any(static n => n is IdentifierNameSyntax or MemberAccessExpressionSyntax);
    }
}
