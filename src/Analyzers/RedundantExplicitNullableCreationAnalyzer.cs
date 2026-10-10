using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
        "A value converts to its nullable type without 'new T?(value)' wherever the target type is known. Replaces the ReSharper inspection RedundantExplicitNullableCreation.");

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

        if (creation.ArgumentList is not { Arguments: [{ NameColon: null, Expression: var value }] }
            || value.IsKind(SyntaxKind.DefaultLiteralExpression)
            || model.GetTypeInfo(creation).Type?.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
            return;

        // Only where the target is fixed by the context: the value's own type must not leak into inference, overload choice or a conditional's type.
        if (!IsTargetTyped(creation, model, out var argument))
            return;

        if (argument is not null && Speculation.OwnerOf(argument) is { } owner
            && !Speculation.BindsToSame(model, owner, owner.ReplaceNode(creation, value.WithTriviaFrom(creation))))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, creation.GetLocation()));
    }

    /// <param name="argument">Set when the expression ends up as an argument, whose call then has to keep binding to the same method.</param>
    private static bool IsTargetTyped(ExpressionSyntax expression, SemanticModel model, out ArgumentSyntax? argument)
    {
        argument = null;
        SyntaxNode node = expression;
        while (node.Parent is ParenthesizedExpressionSyntax)
            node = node.Parent;

        switch (node.Parent)
        {
            case ArgumentSyntax passed:
                argument = passed;
                return Speculation.OwnerOf(passed) is not null;
            case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } }:
                return !declaration.Type.IsVar;
            case EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax }:
            case ArrowExpressionClauseSyntax:
            case InitializerExpressionSyntax { Parent: ArrayCreationExpressionSyntax }:
                return true;
            case AssignmentExpressionSyntax assignment:
                return assignment.IsKind(SyntaxKind.SimpleAssignmentExpression);
            case BinaryExpressionSyntax binary:
                return binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression);
            case ReturnStatementSyntax returned:
                return returned.Ancestors().FirstOrDefault(static a => a is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)
                    is not AnonymousFunctionExpressionSyntax enclosing
                    || IsTargetTyped(enclosing, model, out argument);
            case ConditionalExpressionSyntax conditional:
                // 'b ? new int?(x) : null' needs the nullable to have a type at all; the other branch has to give the conditional one.
                var other = conditional.WhenTrue == node ? conditional.WhenFalse : conditional.WhenTrue;
                return model.GetTypeInfo(other).Type is not null && IsTargetTyped(conditional, model, out argument);
            case SwitchExpressionArmSyntax { Parent: SwitchExpressionSyntax switchExpression }:
                return IsTargetTyped(switchExpression, model, out argument);
            case AnonymousFunctionExpressionSyntax lambda:
                return IsTargetTyped(lambda, model, out argument);
            default:
                return false;
        }
    }
}
