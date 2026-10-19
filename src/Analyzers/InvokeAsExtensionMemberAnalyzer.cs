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
        "Calling an extension method through its class reads as a utility call and hides the chain it belongs to; 'list.Count()' says the same. Replaces the ReSharper inspection InvokeAsExtensionMember.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

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

        // A call 'list.Count()' carries the receiver as no argument, so the first argument written as one marks the static form. Only the cheap checks come
        // first: the speculative binding below is what decides, and what costs.
        if (!method.IsExtensionMethod
            || call.Syntax is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } invocation
            || call.Arguments.FirstOrDefault() is not { Syntax: ArgumentSyntax { Expression: var receiver } thisArgument }
            || receiver.IsKind(SyntaxKind.InterpolatedStringExpression)
            || IsOutOfOrder(call, invocation)
            || IsInsideDeclaringType(invocation, method.ContainingType, call.SemanticModel!, context))
            return;

        // The edited tree is built node by node, never parsed from text, so a receiver such as 'a + b' stays one operand without parentheses.
        var rest = invocation.ArgumentList.Arguments.Remove(thisArgument);
        var edited = invocation
            .WithExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, access.OperatorToken, access.Name))
            .WithArgumentList(invocation.ArgumentList.WithArguments(rest));

        // The instance form has to find the same extension method: an instance member, another extension or a missing import would change the call.
        if (Speculation.BoundAfter(call.SemanticModel!, invocation, edited, context.CancellationToken) is IMethodSymbol bound
            && SymbolEqualityComparer.Default.Equals(bound.ReducedFrom, method.OriginalDefinition))
            context.ReportDiagnostic(Diagnostic.Create(Rule, access.Name.GetLocation(), method.Name));
    }

    // Written out of order, the arguments are evaluated in that order: 'Ext.M(b: F(), a: H())' would call H before F once 'a' moves to the front.
    // ReSharper stays silent on every such call, and reports the ones written in order, mixed positional and named included.
    private static bool IsOutOfOrder(IInvocationOperation call, InvocationExpressionSyntax invocation)
    {
        var written = invocation.ArgumentList.Arguments
            .Select(argument => call.Arguments.FirstOrDefault(candidate => candidate.Syntax == argument)?.Parameter?.Ordinal)
            .OfType<int>()
            .ToList();

        return written.Zip(written.Skip(1), static (before, after) => after < before).Any(static outOfOrder => outOfOrder);
    }

    // ReSharper stays silent inside the static class that declares the extension method.
    private static bool IsInsideDeclaringType(SyntaxNode invocation, INamedTypeSymbol declaring, SemanticModel model, OperationAnalysisContext context)
    {
        for (var type = model.GetEnclosingSymbol(invocation.SpanStart, context.CancellationToken)?.ContainingType; type is not null; type = type.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(type, declaring))
                return true;
        }

        return false;
    }
}
