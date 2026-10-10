using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FEx.Analyzers;

/// <summary>Tells a place that only stores a value apart from one that runs code when it is read or written.</summary>
internal static class PlainStorage
{
    /// <summary>
    /// Whether <paramref name="target"/> is a local, a parameter, a field or an array element, reached through nothing but those:
    /// (a field-like event counts as a field), no call, no property or indexer (whose accessors may do more than store), no <c>++</c> in an index.
    /// </summary>
    public static bool IsPlainStorage(ExpressionSyntax target, SemanticModel model, CancellationToken cancellationToken)
    {
        foreach (var node in target.DescendantNodesAndSelf())
        {
            var plain = node switch
            {
                SimpleNameSyntax or MemberAccessExpressionSyntax =>
                    model.GetSymbolInfo(node, cancellationToken).Symbol is ILocalSymbol or IParameterSymbol or IFieldSymbol or IEventSymbol or INamedTypeSymbol or INamespaceSymbol,
                ElementAccessExpressionSyntax access => model.GetTypeInfo(access.Expression, cancellationToken).Type is IArrayTypeSymbol,
                ThisExpressionSyntax or ParenthesizedExpressionSyntax or LiteralExpressionSyntax or BracketedArgumentListSyntax or ArgumentSyntax
                    or PredefinedTypeSyntax or QualifiedNameSyntax or BinaryExpressionSyntax => true,
                _ => false,
            };
            if (!plain)
                return false;
        }

        return true;
    }
}
