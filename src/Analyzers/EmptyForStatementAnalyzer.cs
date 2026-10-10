using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0012: a <c>for</c> with an empty body whose header does nothing but count.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyForStatementAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0012";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Empty for statement",
        "This 'for' has an empty body and a side-effect-free header, so it does nothing",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "A for loop with an empty body, a condition, and a header that only reads values and advances its own counters has no effect. A header that calls a method, creates an object or writes anything else is left alone. Replaces the ReSharper inspection EmptyForStatement.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ForStatement);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var loop = (ForStatementSyntax)context.Node;
        if (loop.Condition is null || !IsEmptyBody(loop.Statement) || loop.Declaration?.Type is RefTypeSyntax)
            return;

        var header = new List<SyntaxNode>();
        if (loop.Declaration is not null)
            header.Add(loop.Declaration);
        header.AddRange(loop.Initializers);
        header.Add(loop.Condition);
        header.AddRange(loop.Incrementors);

        if (header.All(part => part.DescendantNodesAndSelf().All(node => IsPure(context.SemanticModel, loop, node))))
            context.ReportDiagnostic(Diagnostic.Create(Rule, loop.GetLocation()));
    }

    private static bool IsEmptyBody(StatementSyntax body) =>
        body is EmptyStatementSyntax || body is BlockSyntax block && block.Statements.All(statement => statement is EmptyStatementSyntax);

    private static bool IsPure(SemanticModel model, ForStatementSyntax loop, SyntaxNode node) => node switch
    {
        AssignmentExpressionSyntax assignment => IsOwnCounter(model, loop, assignment.Left),
        PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression) =>
            IsOwnCounter(model, loop, prefix.Operand),
        PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.PostIncrementExpression) || postfix.IsKind(SyntaxKind.PostDecrementExpression) =>
            IsOwnCounter(model, loop, postfix.Operand),
        BaseObjectCreationExpressionSyntax or AnonymousFunctionExpressionSyntax or AwaitExpressionSyntax or ThrowExpressionSyntax => false,
        InvocationExpressionSyntax invocation => IsPureCall(model, invocation),
        _ => true,
    };

    // Only a local the for declares itself may change; anything else the header writes is a side effect.
    private static bool IsOwnCounter(SemanticModel model, ForStatementSyntax loop, ExpressionSyntax target)
    {
        if (loop.Declaration is null || target is not IdentifierNameSyntax name || model.GetSymbolInfo(name).Symbol is not { } symbol)
            return false;

        return symbol.Locations.Any(location => location.SourceTree == loop.SyntaxTree && loop.Declaration.Span.Contains(location.SourceSpan));
    }

    private static bool IsPureCall(SemanticModel model, InvocationExpressionSyntax invocation) =>
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
        && (method.ContainingType.SpecialType == SpecialType.System_String || method.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == "PureAttribute"));
}
