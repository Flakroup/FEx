using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FEx.Analyzers;

/// <summary>What FEX0037 and FEX0038 share: whether a name declared in a body hides a field or property of the enclosing type.</summary>
internal static class MemberHiding
{
    /// <summary>
    /// Whether a field (const included) or property named <paramref name="name"/>, declared by the type that
    /// <paramref name="container"/> sits in or by one of its base types, is reachable from there and hidden by the new name.
    /// Members of outer types are not counted, and a static context only sees static members.
    /// </summary>
    public static bool Hides(Compilation compilation, ISymbol container, string name)
    {
        var staticContext = false;
        var owner = container;
        while (owner is not INamedTypeSymbol)
        {
            // The statements of a top-level program are one static method, whose names ReSharper leaves alone; a lambda in
            // an attribute argument has no enclosing type at all.
            if (owner is null or IMethodSymbol { Name: WellKnownMemberNames.TopLevelStatementsEntryPointMethodName })
                return false;

            staticContext |= owner.IsStatic;
            owner = owner.ContainingSymbol;
        }

        var type = (INamedTypeSymbol)owner;
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers(name))
            {
                if (member is IFieldSymbol or IPropertySymbol
                    && (member.IsStatic || !staticContext)
                    && compilation.IsSymbolAccessibleWithin(member, type))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A parameter of something that has no body cannot hide anything: it names no variable a body could mistake.
    /// Nor can one of a partial method's implementation whose declaring part sits in the same type declaration.
    /// </summary>
    public static bool IsBodiless(ParameterSyntax parameter) =>
        parameter.Parent?.Parent switch
        {
            MethodDeclarationSyntax method => method.Body is null && method.ExpressionBody is null,
            OperatorDeclarationSyntax @operator => @operator.Body is null && @operator.ExpressionBody is null,
            ConversionOperatorDeclarationSyntax conversion => conversion.Body is null && conversion.ExpressionBody is null,
            ConstructorDeclarationSyntax or TypeDeclarationSyntax or DelegateDeclarationSyntax => true,
            _ => false,
        };

    /// <summary>Whether <paramref name="parameter"/> belongs to a partial method implementation whose definition is in the same type declaration.</summary>
    public static bool IsPartOfOneDeclaration(IParameterSymbol parameter, CancellationToken cancellationToken) =>
        parameter.ContainingSymbol is IMethodSymbol { PartialDefinitionPart: { } definition } implementation
        && implementation.DeclaringSyntaxReferences.Any(reference => definition.DeclaringSyntaxReferences.Any(other => other.GetSyntax(cancellationToken).Parent == reference.GetSyntax(cancellationToken).Parent));
}
