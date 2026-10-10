using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FEx.Analyzers;

/// <summary>Asks the compiler what a call would bind to if an argument were gone or written differently, because dropping it can silently pick another overload.</summary>
internal static class Speculation
{
    /// <summary>The call, initializer or attribute that takes <paramref name="argument"/>, or <c>null</c> when it is not one the binder can be asked about.</summary>
    public static SyntaxNode? OwnerOf(SyntaxNode argument) => argument.Parent switch
    {
        ArgumentListSyntax or BracketedArgumentListSyntax or AttributeArgumentListSyntax => argument.Parent.Parent,
        _ => null,
    };

    /// <summary>Whether <paramref name="owner"/> still binds to the same symbol once <paramref name="edited"/> (a rewrite of it) stands in its place.</summary>
    public static bool BindsToSame(SemanticModel model, SyntaxNode owner, SyntaxNode edited)
    {
        // 'o?.M(x)' has no standalone form to bind; ask about 'o.M(x)' instead. A chain after it ('o?.M(x).N()') is not asked about at all.
        if (owner is InvocationExpressionSyntax { Expression: MemberBindingExpressionSyntax binding })
        {
            if (owner.Parent is not ConditionalAccessExpressionSyntax access)
                return false;

            edited = ((InvocationExpressionSyntax)edited).WithExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, access.Expression, binding.Name));
        }

        if (model.GetSymbolInfo(owner).Symbol is not { } before)
            return false;

        // 'new(x)' takes its type from the context around it, which a lone speculative node lacks, so spell the type out.
        if (edited is ImplicitObjectCreationExpressionSyntax implicitCreation)
        {
            // The constructor it bound to belongs to the type the expression creates.
            var typeName = before.ContainingType!.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToMinimalDisplayString(model, owner.SpanStart);
            edited = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(typeName), implicitCreation.ArgumentList, implicitCreation.Initializer);
        }

        var position = owner.SpanStart;
        var after = edited switch
        {
            ConstructorInitializerSyntax initializer => model.GetSpeculativeSymbolInfo(position, initializer).Symbol,
            AttributeSyntax attribute => model.GetSpeculativeSymbolInfo(position, attribute).Symbol,
            _ => model.GetSpeculativeSymbolInfo(position, (ExpressionSyntax)edited, SpeculativeBindingOption.BindAsExpression).Symbol,
        };
        return SymbolEqualityComparer.Default.Equals(before, after);
    }
}
