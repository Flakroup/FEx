using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FEx.Analyzers;

/// <summary>FEX0056: an extension method called as a plain static method, <c>Enumerable.Count(list)</c>, where <c>list.Count()</c> binds to the same method.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InvokeAsExtensionMemberAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0056";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Invoke as extension member",
        "'{0}' is an extension method and can be invoked on its first argument",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "Calling an extension method through its class reads as a utility call and hides the chain it belongs to; 'list.Count()' says the same. Replaces the ReSharper inspection InvokeAsExtensionMember.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context)
    {
        var call = (IInvocationOperation)context.Operation;
        var method = call.TargetMethod;

        // The static form has the unreduced method; 'list.Count()' already carries the reduced one.
        if (!method.IsExtensionMethod
            || method.ReducedFrom is not null
            || call.Syntax is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } access } invocation
            || call.Arguments.FirstOrDefault(static argument => argument.Parameter?.Ordinal == 0) is not { Syntax: ArgumentSyntax { Expression: var receiver } thisArgument }
            || ReceiverIsUnsuitable(receiver)
            || !NamesType(call.SemanticModel!, access.Expression, method.ContainingType, context)
            || IsInsideDeclaringType(invocation, method.ContainingType, call.SemanticModel!, context))
            return;

        var rest = invocation.ArgumentList.Arguments.Remove(thisArgument);
        var edited = invocation
            .WithExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, AsReceiver(receiver.WithoutTrivia()), access.OperatorToken, access.Name))
            .WithArgumentList(invocation.ArgumentList.WithArguments(rest));

        // The instance form has to find the same extension method: an instance member, another extension or a missing import would change the call.
        if (Speculation.BoundAfter(call.SemanticModel!, invocation, edited, context.CancellationToken) is IMethodSymbol bound
            && SymbolEqualityComparer.Default.Equals((bound.ReducedFrom ?? bound).OriginalDefinition, method.OriginalDefinition))
            context.ReportDiagnostic(Diagnostic.Create(Rule, access.Name.GetLocation(), method.Name));
    }

    // ReSharper leaves an interpolated string alone as the receiver, though 'receiver' binds fine.
    private static bool ReceiverIsUnsuitable(ExpressionSyntax receiver) =>
        receiver.IsKind(SyntaxKind.InterpolatedStringExpression)
        || receiver is ParenthesizedExpressionSyntax { Expression: var inner } && ReceiverIsUnsuitable(inner);

    // 'Enumerable' or an alias of it, not an expression that happens to be named like a type.
    private static bool NamesType(SemanticModel model, ExpressionSyntax qualifier, INamedTypeSymbol declaring, OperationAnalysisContext context)
    {
        var symbol = model.GetSymbolInfo(qualifier, context.CancellationToken).Symbol;

        return (symbol is IAliasSymbol alias ? alias.Target : symbol) is INamedTypeSymbol named
               && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, declaring.OriginalDefinition);
    }

    // ReSharper stays silent inside the static class that declares the extension method.
    private static bool IsInsideDeclaringType(SyntaxNode invocation, INamedTypeSymbol declaring, SemanticModel model, OperationAnalysisContext context)
    {
        for (var type = model.GetEnclosingSymbol(invocation.SpanStart, context.CancellationToken)?.ContainingType; type is not null; type = type.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, declaring.OriginalDefinition))
                return true;
        }

        return false;
    }

    private static ExpressionSyntax AsReceiver(ExpressionSyntax receiver) =>
        receiver.Kind() is SyntaxKind.IdentifierName or SyntaxKind.GenericName or SyntaxKind.SimpleMemberAccessExpression or SyntaxKind.InvocationExpression
            or SyntaxKind.ElementAccessExpression or SyntaxKind.ThisExpression or SyntaxKind.BaseExpression or SyntaxKind.ParenthesizedExpression
            or SyntaxKind.ObjectCreationExpression or SyntaxKind.DefaultExpression or SyntaxKind.TypeOfExpression or SyntaxKind.StringLiteralExpression
            or SyntaxKind.CharacterLiteralExpression or SyntaxKind.SuppressNullableWarningExpression
            ? receiver
            : SyntaxFactory.ParenthesizedExpression(receiver);
}
