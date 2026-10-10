using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace FEx.Analyzers;

/// <summary>What several analyzers need to agree on, so that two rules never disagree about the same code.</summary>
internal static class AnalyzerHelpers
{
    /// <summary>
    /// A member no other type can reach: private, protected or private protected in a sealed class, or anything declared
    /// in a private nested type. ReSharper reports its "local" inspections for exactly these.
    /// </summary>
    public static bool IsEffectivelyPrivate(ISymbol symbol)
    {
        for (var type = symbol.ContainingType; type is not null; type = type.ContainingType)
        {
            if (type.DeclaredAccessibility == Accessibility.Private)
                return true;
        }

        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Private => true,
            Accessibility.Protected or Accessibility.ProtectedAndInternal => symbol.ContainingType.IsSealed,
            _ => false,
        };
    }

    /// <summary>The types a <c>const</c> may have: the primitives, <c>string</c> and enums.</summary>
    public static bool IsConstantType(ITypeSymbol type) =>
        type.TypeKind == TypeKind.Enum
        || type.SpecialType is SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_SByte
            or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32
            or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Single
            or SpecialType.System_Double or SpecialType.System_Decimal or SpecialType.System_String;

    /// <summary>
    /// Whether two types are the same, the type parameters of two different generic methods being the same when they sit
    /// at the same position: <c>T</c> of <c>M&lt;T&gt;(T a)</c> is the <c>T</c> of <c>M&lt;T&gt;(T a, int b)</c>.
    /// </summary>
    public static bool SameType(ITypeSymbol left, ITypeSymbol right) =>
        SymbolEqualityComparer.Default.Equals(left, right)
        || (left, right) switch
        {
            (ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } first, ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } second) => first.Ordinal == second.Ordinal,
            (IArrayTypeSymbol first, IArrayTypeSymbol second) => first.Rank == second.Rank && SameType(first.ElementType, second.ElementType),
            (INamedTypeSymbol { IsGenericType: true } first, INamedTypeSymbol { IsGenericType: true } second) =>
                SymbolEqualityComparer.Default.Equals(first.OriginalDefinition, second.OriginalDefinition)
                && first.TypeArguments.Zip(second.TypeArguments, SameType).All(same => same),
            _ => false,
        };

    /// <summary>Whether two compile-time values are the same value, whatever numeric type each was written as.</summary>
    public static bool ValuesEqual(object? left, object? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        if (!IsNumber(left) || !IsNumber(right))
            return left.Equals(right);

        return left is float or double || right is float or double
            ? Convert.ToDouble(left) == Convert.ToDouble(right)
            : Convert.ToDecimal(left) == Convert.ToDecimal(right);
    }

    /// <summary>Whether two parameters have the same default: both none, or both the same value.</summary>
    public static bool DefaultsEqual(IParameterSymbol left, IParameterSymbol right) =>
        left.HasExplicitDefaultValue == right.HasExplicitDefaultValue
        && (!left.HasExplicitDefaultValue || ValuesEqual(left.ExplicitDefaultValue, right.ExplicitDefaultValue));

    public static bool IsInsideNameOf(IOperation operation)
    {
        for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is INameOfOperation)
                return true;
        }

        return false;
    }

    /// <summary>Whether a reference to a variable stores to it, or hands out a way to: assignment, increment, address-of, ref argument, ref alias, deconstruction.</summary>
    public static bool IsWrite(IOperation reference)
    {
        var child = reference;
        var parent = reference.Parent;
        while (parent is ITupleOperation)
        {
            child = parent;
            parent = parent.Parent;
        }

        return parent switch
        {
            IAssignmentOperation assignment => assignment.Target == child,
            IIncrementOrDecrementOperation or IAddressOfOperation => true,
            IArgumentOperation argument => argument.Parameter is not { RefKind: RefKind.None },
            _ => reference.Syntax.Parent is RefExpressionSyntax,
        };
    }

    /// <summary>Whether <paramref name="symbol"/> implements a member of one of its type's interfaces, implicitly or explicitly.</summary>
    public static bool ImplementsInterfaceMember(ISymbol symbol)
    {
        var type = symbol.ContainingType;

        return type.AllInterfaces
            .SelectMany(contract => contract.GetMembers())
            .Any(member => SymbolEqualityComparer.Default.Equals(type.FindImplementationForInterfaceMember(member), symbol));
    }

    private static bool IsNumber(object value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
}
