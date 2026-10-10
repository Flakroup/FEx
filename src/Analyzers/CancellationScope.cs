using System.Linq;
using Microsoft.CodeAnalysis;

namespace FEx.Analyzers;

/// <summary>What FEX0052 and FEX0053 share: whether the code at an operation could pass a <see cref="System.Threading.CancellationToken"/> without declaring one.</summary>
internal static class CancellationScope
{
    public const string TokenMetadataName = "System.Threading.CancellationToken";

    /// <summary>
    /// Whether a parameter of the enclosing functions, a local declared before the operation, or - unless the enclosing
    /// method is static - an instance field or property of the enclosing type itself holds a token.
    /// </summary>
    public static bool HasTokenInScope(IOperation operation, ISymbol containingSymbol, INamedTypeSymbol token)
    {
        var position = operation.Syntax.SpanStart;
        var inScope = operation.SemanticModel!.LookupSymbols(position).Any(symbol => symbol switch
        {
            IParameterSymbol parameter => IsToken(parameter.Type, token),
            ILocalSymbol local => IsToken(local.Type, token) && local.Locations[0].SourceSpan.End <= position,
            _ => false,
        });

        // An indexer is no name in scope, yet ReSharper counts it with the other members.
        return inScope
               || !containingSymbol.IsStatic
               && containingSymbol.ContainingType.GetMembers().Any(member => !member.IsStatic && member switch
               {
                   IFieldSymbol field => IsToken(field.Type, token),
                   IPropertySymbol property => IsToken(property.Type, token),
                   _ => false,
               });
    }

    public static bool IsToken(ITypeSymbol? type, INamedTypeSymbol token) => SymbolEqualityComparer.Default.Equals(type, token);
}
