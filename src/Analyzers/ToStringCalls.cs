using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Threading;

namespace FEx.Analyzers;

/// <summary>What FEX0022 and FEX0023 share: a parameterless <c>ToString()</c> call and the places where the surrounding code converts the receiver to a string itself.</summary>
internal static class ToStringCalls
{
    /// <summary>
    /// Whether <paramref name="call"/> is a parameterless <c>ToString()</c> on a receiver of the kind the rule is about
    /// (a value type, or a type that may be one, versus a reference type) in a place that stringifies the receiver anyway.
    /// </summary>
    /// <remarks>Removing the call has to bind the same way and print the same text, so every place asks the compiler instead of trusting the shape of the code.</remarks>
    public static bool IsRedundant(InvocationExpressionSyntax call, SemanticModel model, bool valueTypeReceiver, CancellationToken cancellationToken)
    {
        if (model.GetSymbolInfo(call, cancellationToken).Symbol is not IMethodSymbol { Name: "ToString", Parameters.IsEmpty: true } method
            || RootOverridden(method).ContainingType.SpecialType != SpecialType.System_Object
            || ReceiverOf(call) is not { } receiver
            || receiver is BaseExpressionSyntax
            || model.GetTypeInfo(receiver, cancellationToken).Type is not { } type
            || type.IsReferenceType == valueTypeReceiver
            || type.IsRefLikeType
            || ConvertsToStringByUserCode(model.Compilation, type))
        {
            return false;
        }

        // string.ToString() returns the instance itself, wherever it stands.
        if (type.SpecialType == SpecialType.System_String)
            return true;

        // The node the call sits in, and what stands there once the call is gone: 'o?.ToString()' loses its '?.', 'o?.P.ToString()' keeps 'o?.P'.
        SyntaxNode target = call;
        var stripped = receiver;
        if (call.Parent is ConditionalAccessExpressionSyntax conditional && conditional.WhenNotNull == call)
        {
            target = conditional;
            stripped = call.Expression is MemberAccessExpressionSyntax access ? conditional.WithWhenNotNull(access.Expression) : conditional.Expression;
        }

        var node = target;
        while (node.Parent is ParenthesizedExpressionSyntax)
            node = node.Parent;

        return node.Parent switch
        {
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression) =>
                IsStringConcatenationAfterwards(model, binary, binary.Left == node ? binary.Right : binary.Left, target, stripped, cancellationToken),
            // Boxing a value type is the reason to keep ToString() in an interpolation hole or an argument, so only reference types are reported there.
            InterpolationSyntax interpolation => !valueTypeReceiver && IsPlainHole(model, interpolation, cancellationToken),
            ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax outer } argument =>
                !valueTypeReceiver && IsStringifyingParameter(model, outer, argument, target, stripped, type, cancellationToken),
            _ => false,
        };
    }

    private static ExpressionSyntax? ReceiverOf(InvocationExpressionSyntax call) => call.Expression switch
    {
        MemberAccessExpressionSyntax access => access.Expression,
        MemberBindingExpressionSyntax when call.Parent is ConditionalAccessExpressionSyntax conditional => conditional.Expression,
        _ => null,
    };

    // A ToString that hides object's (or that an interface declares anew) is not what the string conversion calls.
    private static IMethodSymbol RootOverridden(IMethodSymbol method)
    {
        while (method.OverriddenMethod is { } overridden)
            method = overridden;

        return method;
    }

    // 'a + x' calls the conversion where 'a + x.ToString()' calls ToString.
    private static bool ConvertsToStringByUserCode(Compilation compilation, ITypeSymbol type) =>
        compilation.ClassifyCommonConversion(type, compilation.GetSpecialType(SpecialType.System_String)) is { IsImplicit: true, IsUserDefined: true };

    // Both sides of 'i.ToString() + j.ToString()' look redundant, yet 'i + j' adds numbers; the other side has to be a string for another reason.
    private static bool IsStringConcatenationAfterwards(
        SemanticModel model,
        BinaryExpressionSyntax binary,
        ExpressionSyntax other,
        SyntaxNode target,
        ExpressionSyntax stripped,
        CancellationToken cancellationToken)
    {
        if (model.GetTypeInfo(other, cancellationToken).Type?.SpecialType != SpecialType.System_String
            || IsToStringOfNonString(other, model, cancellationToken))
        {
            return false;
        }

        // A user-defined 'operator +(string, T)' would take over, and so would a bound that is no longer a string concatenation.
        return Speculation.BoundAfter(model, binary, binary.ReplaceNode(target, stripped), cancellationToken)
            is IMethodSymbol { MethodKind: MethodKind.BuiltinOperator, ReturnType.SpecialType: SpecialType.System_String };
    }

    private static bool IsToStringOfNonString(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        if (expression is ConditionalAccessExpressionSyntax { WhenNotNull: InvocationExpressionSyntax conditionalCall })
            expression = conditionalCall;

        return expression is InvocationExpressionSyntax call
            && model.GetSymbolInfo(call, cancellationToken).Symbol is IMethodSymbol { Name: "ToString", Parameters.IsEmpty: true }
            && ReceiverOf(call) is { } receiver
            && model.GetTypeInfo(receiver, cancellationToken).Type?.SpecialType != SpecialType.System_String;
    }

    // A format or a provider is applied to the receiver, not to the string it prints; and a handler or a FormattableString formats the receiver itself.
    private static bool IsPlainHole(SemanticModel model, InterpolationSyntax hole, CancellationToken cancellationToken) =>
        hole is { FormatClause: null, Parent: InterpolatedStringExpressionSyntax interpolated }
        && model.GetConversion(interpolated, cancellationToken) is { IsInterpolatedString: false, IsInterpolatedStringHandler: false };

    // string.Format(format, args) formats every argument itself, and StringBuilder.Append(string) is what Append(object) ends in.
    private static bool IsStringifyingParameter(
        SemanticModel model,
        InvocationExpressionSyntax outer,
        ArgumentSyntax argument,
        SyntaxNode target,
        ExpressionSyntax stripped,
        ITypeSymbol type,
        CancellationToken cancellationToken)
    {
        if (model.GetSymbolInfo(outer, cancellationToken).Symbol is not IMethodSymbol { ContainingType: { } owner, Parameters: [{ Type.SpecialType: SpecialType.System_String }, ..] } method
            || ParameterOf(outer, argument, method) is not { } parameter)
        {
            return false;
        }

        var edited = outer.ReplaceNode(target, stripped);
        // An array would be taken as the params array itself.
        if (owner.SpecialType == SpecialType.System_String && method.Name == "Format")
            return parameter.Ordinal != 0 && type is not IArrayTypeSymbol && Speculation.BindsToSame(model, outer, edited, cancellationToken);

        // Append(string) becomes whichever overload the receiver fits best, which only Append(object) reads the same way.
        return owner.Name == "StringBuilder" && method.Name == "Append" && method.Parameters.Length == 1
            && Speculation.BoundAfter(model, outer, edited, cancellationToken) is IMethodSymbol { Parameters: [{ Type.SpecialType: SpecialType.System_Object }] };
    }

    // Named arguments need not come in parameter order, so the position alone says nothing.
    private static IParameterSymbol? ParameterOf(InvocationExpressionSyntax outer, ArgumentSyntax argument, IMethodSymbol method)
    {
        if (argument.NameColon is { } name)
            return method.Parameters.FirstOrDefault(p => p.Name == name.Name.Identifier.ValueText);

        // Past the last parameter, a params array takes the argument.
        var index = outer.ArgumentList.Arguments.IndexOf(argument);
        return method.Parameters[Math.Min(index, method.Parameters.Length - 1)];
    }
}
