using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FEx.Analyzers;

/// <summary>Asks the compiler what a call would bind to if an argument were gone or written differently, because dropping it can silently pick another overload.</summary>
internal static class Speculation
{
    /// <summary>The call, indexer access or constructor initializer that takes <paramref name="argument"/>, or <c>null</c> when it is not one the binder can be asked about.</summary>
    public static SyntaxNode? OwnerOf(SyntaxNode argument) => argument.Parent is ArgumentListSyntax or BracketedArgumentListSyntax
        && argument.Parent.Parent is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax or ElementAccessExpressionSyntax or ElementBindingExpressionSyntax or ConstructorInitializerSyntax
            ? argument.Parent.Parent
            : null;

    /// <summary>
    /// The symbol <paramref name="owner"/> binds to once <paramref name="edited"/> (a rewrite of it) stands in its place,
    /// or <c>null</c> when the compiler cannot be asked: the rewrite is cut out of a null-conditional chain it needs.
    /// </summary>
    public static ISymbol? BoundAfter(SemanticModel model, SyntaxNode owner, SyntaxNode edited, CancellationToken cancellationToken)
    {
        // 'o?.M(x)' has no standalone form to bind; ask about 'o.M(x)' instead.
        if (owner is InvocationExpressionSyntax { Expression: MemberBindingExpressionSyntax binding } && owner.Parent is ConditionalAccessExpressionSyntax access)
        {
            edited = ((InvocationExpressionSyntax)edited).WithExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, access.Expression, binding.Name));
        }

        // A '.x' or '[x]' whose '?' is outside the rewrite has nothing to bind against, and the binder throws on it.
        if (HasDanglingBinding(edited))
            return null;

        // 'new(x)' takes its type from the context around it, which a lone speculative node lacks, so spell the type out.
        if (edited is ImplicitObjectCreationExpressionSyntax implicitCreation)
        {
            // The constructor it bound to belongs to the type the expression creates; with no constructor the name is empty and nothing binds against it.
            var typeName = model.GetSymbolInfo(owner, cancellationToken).Symbol?.ContainingType?.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToMinimalDisplayString(model, owner.SpanStart) ?? string.Empty;
            edited = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(typeName), implicitCreation.ArgumentList, implicitCreation.Initializer);
        }

        var position = owner.SpanStart;
        return edited switch
        {
            ConstructorInitializerSyntax initializer => model.GetSpeculativeSymbolInfo(position, initializer).Symbol,
            AttributeSyntax attribute => model.GetSpeculativeSymbolInfo(position, attribute).Symbol,
            _ => model.GetSpeculativeSymbolInfo(position, (ExpressionSyntax)edited, SpeculativeBindingOption.BindAsExpression).Symbol,
        };
    }

    /// <summary>Whether <paramref name="owner"/> still binds to the same symbol once <paramref name="edited"/> (a rewrite of it) stands in its place.</summary>
    public static bool BindsToSame(SemanticModel model, SyntaxNode owner, SyntaxNode edited, CancellationToken cancellationToken) =>
        model.GetSymbolInfo(owner, cancellationToken).Symbol is { } before
        && SymbolEqualityComparer.Default.Equals(before, BoundAfter(model, owner, edited, cancellationToken));

    // A member or element binding belongs to the '?' whose right-hand side holds it; a node cut out of that '?' keeps the binding and loses what it binds to.
    private static bool HasDanglingBinding(SyntaxNode node) =>
        node.DescendantNodesAndSelf().Any(static n => n is MemberBindingExpressionSyntax or ElementBindingExpressionSyntax && !IsBoundWithin(n));

    private static bool IsBoundWithin(SyntaxNode binding)
    {
        var child = binding;
        while (child.Parent is { } parent)
        {
            if (parent is ConditionalAccessExpressionSyntax access && access.WhenNotNull == child)
                return true;

            child = parent;
        }

        return false;
    }
}
