using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FEx.Analyzers;

/// <summary>What FEX0022 and FEX0023 share: a parameterless <c>ToString()</c> call and the places where the surrounding code converts the receiver to a string itself.</summary>
internal static class ToStringCalls
{
    /// <summary>
    /// Whether <paramref name="call"/> is a parameterless <c>ToString()</c> on a receiver of the kind the rule is about
    /// (a value type, or a type that may be one, versus a reference type) in a place that stringifies the receiver anyway.
    /// </summary>
    public static bool IsRedundant(InvocationExpressionSyntax call, SemanticModel model, bool valueTypeReceiver)
    {
        if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol { Name: "ToString", Parameters.IsEmpty: true }
            || ReceiverOf(call) is not { } receiver
            || receiver is BaseExpressionSyntax
            || model.GetTypeInfo(receiver).Type is not { } type
            || type.IsReferenceType == valueTypeReceiver)
            return false;

        // string.ToString() returns the instance itself, wherever it stands.
        if (type.SpecialType == SpecialType.System_String)
            return true;

        SyntaxNode node = call;
        if (call.Parent is ConditionalAccessExpressionSyntax conditional && conditional.WhenNotNull == call)
            node = conditional;
        while (node.Parent is ParenthesizedExpressionSyntax)
            node = node.Parent;

        return node.Parent switch
        {
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression) =>
                model.GetTypeInfo(binary.Left == node ? binary.Right : binary.Left).Type?.SpecialType == SpecialType.System_String,
            // Boxing a value type is the reason to keep ToString() in an interpolation hole or an argument, so only reference types are reported there.
            InterpolationSyntax => !valueTypeReceiver,
            ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax outer } argument => !valueTypeReceiver && IsStringifyingParameter(outer, argument, model),
            _ => false,
        };
    }

    private static ExpressionSyntax? ReceiverOf(InvocationExpressionSyntax call) => call.Expression switch
    {
        MemberAccessExpressionSyntax access => access.Expression,
        MemberBindingExpressionSyntax when call.Parent is ConditionalAccessExpressionSyntax conditional => conditional.Expression,
        _ => null,
    };

    // string.Format(format, args) formats every argument itself, and StringBuilder.Append(string) is what Append(object) ends in.
    private static bool IsStringifyingParameter(InvocationExpressionSyntax outer, ArgumentSyntax argument, SemanticModel model) =>
        model.GetSymbolInfo(outer).Symbol is IMethodSymbol { ContainingType: { } owner } method
        && method.Parameters[0].Type.SpecialType == SpecialType.System_String
        && (owner.SpecialType == SpecialType.System_String && method.Name == "Format" && outer.ArgumentList.Arguments[0] != argument
            || owner.Name == "StringBuilder" && method.Name == "Append" && method.Parameters.Length == 1);
}
