using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0028: <c>new D(handler)</c> where the context already converts <c>handler</c> to a delegate type.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantDelegateCreationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0028";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant delegate creation",
        "The delegate creation is redundant: the method group, lambda or delegate converts to the target type implicitly",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "A method group, lambda or compatible delegate converts to a delegate type without 'new'. Replaces the ReSharper inspection RedundantDelegateCreation.");

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

        if (creation.ArgumentList is not { Arguments: [var argument] }
            || model.GetTypeInfo(creation) is not { Type.TypeKind: TypeKind.Delegate, ConvertedType.TypeKind: TypeKind.Delegate } info)
            return;

        SyntaxNode node = creation;
        while (node.Parent is ParenthesizedExpressionSyntax)
            node = node.Parent;

        // Where the delegate is itself the operand, or the context would infer a type from it, the creation is what fixes the type.
        // A binary operator other than ?? has no target type to convert a bare method group or lambda to.
        if (node.Parent is MemberAccessExpressionSyntax or ConditionalAccessExpressionSyntax or ConditionalExpressionSyntax
            || node.Parent is BinaryExpressionSyntax binary && !binary.IsKind(SyntaxKind.CoalesceExpression)
            || node.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type.IsVar: true } } })
            return;

        if (!model.ClassifyConversion(argument.Expression, info.ConvertedType).IsImplicit)
            return;

        if (node.Parent is ArgumentSyntax passed && Speculation.OwnerOf(passed) is { } owner
            && !Speculation.BindsToSame(model, owner, owner.ReplaceNode(creation, argument.Expression.WithTriviaFrom(creation))))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, creation.GetLocation()));
    }
}
