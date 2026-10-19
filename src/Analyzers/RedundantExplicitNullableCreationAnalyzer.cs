using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace FEx.Analyzers;

/// <summary>FEX0030: <c>new int?(x)</c> or <c>new Nullable&lt;int&gt;(x)</c> in a place that converts <c>x</c> to the nullable type anyway.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantExplicitNullableCreationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0030";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant explicit nullable creation",
        "The explicit nullable creation is redundant: the value converts to the nullable type implicitly",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "A value converts to its nullable type without 'new T?(value)' wherever the target type is known. Replaces the ReSharper inspection RedundantExplicitNullableCreation.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ObjectCreationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var creation = (ObjectCreationExpressionSyntax)context.Node;
        var model = context.SemanticModel;
        var cancellationToken = context.CancellationToken;

        if (creation.ArgumentList is not { Arguments: [{ NameColon: null, Expression: var value }] }
            || Unparenthesize(value).IsKind(SyntaxKind.DefaultLiteralExpression)
            || model.GetTypeInfo(creation, cancellationToken).Type?.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
        {
            return;
        }

        // Only where the target is fixed by the context: the value's own type must not leak into inference, overload choice or a conditional's type.
        if (!IsTargetTyped(creation, model, cancellationToken, out var owner))
            return;

        if (owner is not null && !Speculation.BindsToSame(model, owner, owner.ReplaceNode(creation, value.WithTriviaFrom(creation)), cancellationToken))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, creation.GetLocation()));
    }

    private static ExpressionSyntax Unparenthesize(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        return expression;
    }

    // 'default' and 'null' take their type from the target; next to the plain value they would take (or lack) the value's own type, and 'default' would turn from null into 0.
    private static bool IsTypeLessLiteral(ExpressionSyntax expression) =>
        Unparenthesize(expression).Kind() is SyntaxKind.DefaultLiteralExpression or SyntaxKind.NullLiteralExpression;

    // A void-returning delegate has no value to convert, and a plain Delegate or an expression tree is built from the lambda's own type.
    private static bool ReturnsValueToDelegate(AnonymousFunctionExpressionSyntax lambda, SemanticModel model, CancellationToken cancellationToken) =>
        model.GetTypeInfo(lambda, cancellationToken).ConvertedType is INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod.ReturnsVoid: false };

    /// <param name="owner">Set when the call or operator around the expression has to keep binding to the same member once the creation is gone.</param>
    private static bool IsTargetTyped(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken, out SyntaxNode? owner)
    {
        owner = null;
        SyntaxNode node = expression;
        while (node.Parent is ParenthesizedExpressionSyntax)
            node = node.Parent;

        switch (node.Parent)
        {
            case ArgumentSyntax passed:
                owner = Speculation.OwnerOf(passed);
                return owner is not null;
            case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } }:
                return !declaration.Type.IsVar;
            case EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax }:
            case InitializerExpressionSyntax { Parent: ArrayCreationExpressionSyntax }:
                return true;
            case ArrowExpressionClauseSyntax { Parent: MethodDeclarationSyntax { ReturnType: PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.VoidKeyword } } }:
            case ArrowExpressionClauseSyntax { Parent: LocalFunctionStatementSyntax { ReturnType: PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.VoidKeyword } } }:
                return false;
            case ArrowExpressionClauseSyntax:
                return true;
            case AssignmentExpressionSyntax assignment:
                return assignment.IsKind(SyntaxKind.SimpleAssignmentExpression);
            case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression):
                // 'x == null' for an int is a constant comparison the compiler rejects, and an operator overload may take the plain value differently.
                owner = binary;
                return !IsTypeLessLiteral(binary.Left == node ? binary.Right : binary.Left);
            case ReturnStatementSyntax returned:
                return returned.Ancestors().FirstOrDefault(static a => a is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)
                    is not AnonymousFunctionExpressionSyntax enclosing
                    || ReturnsValueToDelegate(enclosing, model, cancellationToken) && IsTargetTyped(enclosing, model, cancellationToken, out owner);
            case ConditionalExpressionSyntax conditional:
                // 'b ? new int?(x) : null' needs the nullable to have a type at all; the other branch has to give the conditional one.
                var other = conditional.WhenTrue == node ? conditional.WhenFalse : conditional.WhenTrue;
                return !IsTypeLessLiteral(other)
                    && model.GetTypeInfo(other, cancellationToken).Type is not null
                    && IsTargetTyped(conditional, model, cancellationToken, out owner);
            case SwitchExpressionArmSyntax { Parent: SwitchExpressionSyntax switchExpression }:
                return switchExpression.Arms.All(static arm => !IsTypeLessLiteral(arm.Expression))
                    && IsTargetTyped(switchExpression, model, cancellationToken, out owner);
            case AnonymousFunctionExpressionSyntax lambda:
                return ReturnsValueToDelegate(lambda, model, cancellationToken) && IsTargetTyped(lambda, model, cancellationToken, out owner);
            default:
                return false;
        }
    }
}
