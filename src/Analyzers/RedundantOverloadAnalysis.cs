using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FEx.Analyzers;

/// <summary>What FEX0039 and FEX0040 share: finding a method or constructor that does nothing but call a sibling with its own defaults filled in.</summary>
internal static class RedundantOverloadAnalysis
{
    /// <summary>
    /// The method the node declares, when it is a block-bodied overload (or a constructor chaining to <c>this(...)</c>
    /// with an empty body) that forwards its parameters in order to a sibling and fills each remaining parameter of the
    /// sibling with exactly that parameter's default - a call the optional parameter makes unnecessary.
    /// The sibling must be the only way to call it with that many arguments: another overload that accepts as many would
    /// bind differently once this one is gone.
    /// </summary>
    public static IMethodSymbol? FindRedundant(SyntaxNodeAnalysisContext context)
    {
        CSharpSyntaxNode? call = context.Node switch
        {
            MethodDeclarationSyntax { ExpressionBody: null, Body.Statements: [var statement] } => statement switch
            {
                ExpressionStatementSyntax expression => expression.Expression,
                ReturnStatementSyntax @return => @return.Expression,
                _ => null,
            },
            ConstructorDeclarationSyntax { ExpressionBody: null, Body.Statements.Count: 0, Initializer: { } initializer } => initializer,
            _ => null,
        };

        if (call is null || !IsForwardingCall(call))
            return null;

        var method = (IMethodSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node, context.CancellationToken)!;
        if (method is { IsVirtual: true } or { IsOverride: true }
            || context.SemanticModel.GetOperation(call, context.CancellationToken) is not IInvocationOperation invocation)
            return null;

        var target = invocation.TargetMethod;
        if (!IsSibling(method, target) || !ForwardsWithDefaults(method, invocation) || AnalyzerHelpers.ImplementsInterfaceMember(method))
            return null;

        var siblings = method.ContainingType.GetMembers(method.Name).OfType<IMethodSymbol>();

        return siblings.Any(other => !SymbolEqualityComparer.Default.Equals(other, method) && !SymbolEqualityComparer.Default.Equals(other, target.OriginalDefinition) && CanBeCalledWith(other, method.Parameters.Length))
            ? null
            : method;
    }

    // `M(...)` or `this.M(...)` with no explicit type arguments; anything else (an extension call, another receiver) is a different call.
    private static bool IsForwardingCall(CSharpSyntaxNode call) =>
        call is ConstructorInitializerSyntax
        || call is InvocationExpressionSyntax
        {
            Expression: IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax },
        };

    private static bool IsSibling(IMethodSymbol method, IMethodSymbol target) =>
        method.Name == target.Name
        && (target.Arity == 0 || target.Arity == method.Arity)
        && method.IsStatic == target.IsStatic
        && SymbolEqualityComparer.Default.Equals(method.ContainingType, target.ContainingType)
        && SymbolEqualityComparer.Default.Equals(method.ReturnType, target.ReturnType);

    private static bool ForwardsWithDefaults(IMethodSymbol method, IInvocationOperation invocation)
    {
        var own = method.Parameters;
        var forwarded = invocation.TargetMethod.Parameters;
        if (forwarded.Length <= own.Length)
            return false;

        for (var i = 0; i < own.Length; i++)
        {
            // M(a, b) next to M(a, b = 0) is a different method, not a shorter spelling of the same one.
            if (own[i].IsOptional != forwarded[i].IsOptional
                || own[i].IsOptional && !AnalyzerHelpers.DefaultsEqual(own[i], forwarded[i]))
                return false;
        }

        return invocation.Arguments.All(argument => IsForwardedOrDefault(argument, own, forwarded));
    }

    private static bool IsForwardedOrDefault(IArgumentOperation argument, ImmutableArray<IParameterSymbol> own, ImmutableArray<IParameterSymbol> forwarded)
    {
        var position = argument.Parameter!.Ordinal;

        // A default the call leaves out is the sibling's own, which is exactly what the forwarder must pass.
        return position < own.Length
            ? argument.Value is IParameterReferenceOperation reference && SymbolEqualityComparer.Default.Equals(reference.Parameter, own[position])
            : argument.ArgumentKind == ArgumentKind.DefaultValue
              || argument is { ArgumentKind: ArgumentKind.ParamArray, Value: IArrayCreationOperation { Initializer.ElementValues.IsEmpty: true } }
              || argument.ArgumentKind == ArgumentKind.Explicit
              && forwarded[position].HasExplicitDefaultValue
              && argument.Value.ConstantValue is { HasValue: true } constant
              && AnalyzerHelpers.ValuesEqual(constant.Value, forwarded[position].ExplicitDefaultValue);
    }

    private static bool CanBeCalledWith(IMethodSymbol method, int arguments)
    {
        var parameters = method.Parameters;

        return parameters.Count(parameter => !parameter.IsOptional) <= arguments && arguments <= parameters.Length;
    }
}
