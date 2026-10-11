using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace FEx.Analyzers;

/// <summary>What FEX0052 and FEX0053 share: whether the code at an operation could pass a <see cref="System.Threading.CancellationToken"/> without declaring one.</summary>
internal static class CancellationScope
{
    public const string TokenMetadataName = "System.Threading.CancellationToken";

    /// <summary>
    /// Whether a parameter of the enclosing functions, a local declared before the operation, or - unless the enclosing
    /// method is static - an instance field or property of the enclosing type itself holds a token.
    /// A <c>static</c> lambda or local function sees none of what surrounds it, so only its own parameters and locals count there.
    /// </summary>
    public static bool HasTokenInScope(IOperation operation, ISymbol containingSymbol, INamedTypeSymbol token)
    {
        var position = operation.Syntax.SpanStart;
        var staticFunction = NearestStaticFunction(operation);
        var inScope = operation.SemanticModel!.LookupSymbols(position).Any(symbol => symbol switch
        {
            IParameterSymbol parameter => IsToken(parameter.Type, token) && IsReachable(parameter, staticFunction),
            ILocalSymbol local => IsToken(local.Type, token) && local.Locations[0].SourceSpan.End <= position && IsReachable(local, staticFunction),
            _ => false,
        });

        // An indexer is no name in scope, yet ReSharper counts it with the other members.
        return inScope
               || staticFunction is null
               && !containingSymbol.IsStatic
               && containingSymbol.ContainingType.GetMembers().Any(member => !member.IsStatic && member switch
               {
                   IFieldSymbol field => IsToken(field.Type, token),
                   IPropertySymbol property => IsToken(property.Type, token),
                   _ => false,
               });
    }

    private static IMethodSymbol? NearestStaticFunction(IOperation operation)
    {
        for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            switch (parent)
            {
                case IAnonymousFunctionOperation { Symbol.IsStatic: true } function:
                    return function.Symbol;
                case ILocalFunctionOperation { Symbol.IsStatic: true } local:
                    return local.Symbol;
            }
        }

        return null;
    }

    private static bool IsReachable(ISymbol symbol, IMethodSymbol? staticFunction)
    {
        if (staticFunction is null)
            return true;

        for (var owner = symbol.ContainingSymbol; owner is not null; owner = owner.ContainingSymbol)
        {
            if (SymbolEqualityComparer.Default.Equals(owner, staticFunction))
                return true;
        }

        return false;
    }

    public static bool IsToken(ITypeSymbol? type, INamedTypeSymbol token) => SymbolEqualityComparer.Default.Equals(type, token);
}
