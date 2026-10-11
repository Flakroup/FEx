using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FEx.Analyzers;

/// <summary>What several analyzers need to agree on, so that two rules never disagree about the same code.</summary>
internal static class AnalyzerHelpers
{
    private static readonly string[] GeneratedSuffixes = [".g.cs", ".g.i.cs", ".generated.cs", ".designer.cs"];

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

    /// <summary>
    /// Whether two compile-time values are the same value, whatever numeric type each was written as.
    /// Exactly the same, though: '-0.0' is not '0.0' (1 / x tells them apart), '1.0m' prints differently from '1m', and NaN is NaN.
    /// </summary>
    public static bool ValuesEqual(object? left, object? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        if (!IsNumber(left) || !IsNumber(right))
            return left.Equals(right);

        if (left is not (float or double) && right is not (float or double))
            return decimal.GetBits(Convert.ToDecimal(left)).SequenceEqual(decimal.GetBits(Convert.ToDecimal(right)));

        var (first, second) = (Convert.ToDouble(left), Convert.ToDouble(right));

        return double.IsNaN(first) && double.IsNaN(second) || BitConverter.DoubleToInt64Bits(first) == BitConverter.DoubleToInt64Bits(second);
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

    /// <summary>
    /// Whether a declaration sits in code a tool wrote: a file named like generated output, one whose header says
    /// <c>auto-generated</c>, one the <c>generated_code</c> editorconfig option marks, or a type tagged
    /// <c>GeneratedCode</c> or <c>CompilerGenerated</c>. A rule that reports a fix to hand-written code stays silent there.
    /// </summary>
    public static bool IsGenerated(SyntaxTree tree, ISymbol symbol, AnalyzerOptions options, CancellationToken cancellationToken)
    {
        if (options.AnalyzerConfigOptionsProvider.GetOptions(tree).TryGetValue("generated_code", out var setting) && bool.TryParse(setting, out var configured))
            return configured;

        var path = tree.FilePath;
        if (GeneratedSuffixes.Any(suffix => path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            || Path.GetFileName(path).StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase)
            || tree.GetRoot(cancellationToken).GetFirstToken(true).LeadingTrivia.Any(IsGeneratedBanner))
            return true;

        for (var type = symbol as INamedTypeSymbol ?? symbol.ContainingType; type is not null; type = type.ContainingType)
        {
            if (type.GetAttributes().Any(static attribute => attribute.AttributeClass?.Name is "GeneratedCodeAttribute" or "CompilerGeneratedAttribute"))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Whether code the current configuration excludes (an <c>#if</c> branch that is off) names <paramref name="name"/>.
    /// A partial declaration with no other part may have that part in such a branch, so the report would depend on the build configuration.
    /// </summary>
    public static bool IsNamedInDisabledCode(Compilation compilation, string name, CancellationToken cancellationToken) =>
        compilation.SyntaxTrees
            .Select(tree => tree.GetRoot(cancellationToken))
            .Where(root => root.ContainsDirectives)
            .Any(root => root.DescendantTrivia().Any(trivia => trivia.IsKind(SyntaxKind.DisabledTextTrivia) && trivia.ToString().Contains(name)));

    private static bool IsGeneratedBanner(SyntaxTrivia trivia) =>
        trivia.Kind() is SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia
        && (trivia.ToString().IndexOf("<auto-generated", StringComparison.OrdinalIgnoreCase) >= 0 || trivia.ToString().IndexOf("<autogenerated", StringComparison.OrdinalIgnoreCase) >= 0);

    /// <summary>
    /// Whether a type derived from the one that declares <paramref name="method"/> re-lists an interface the method implements.
    /// A member only a nested private type can reach can only be inherited inside the type that holds it.
    /// </summary>
    public static bool IsImplementedByDerivedType(IMethodSymbol method)
    {
        var holder = method.ContainingType;
        while (holder is { DeclaredAccessibility: not Accessibility.Private })
            holder = holder.ContainingType;

        // An inherited private method implements nothing, and a method nothing can inherit is no concern here.
        return method.DeclaredAccessibility != Accessibility.Private
               && holder?.ContainingType is { } scope
               && DescendantTypes(scope).Any(type => Inherits(type, method.ContainingType) && type.AllInterfaces
                   .SelectMany(contract => contract.GetMembers())
                   .Any(member => SymbolEqualityComparer.Default.Equals(type.FindImplementationForInterfaceMember(member)?.OriginalDefinition, method.OriginalDefinition)));
    }

    private static IEnumerable<INamedTypeSymbol> DescendantTypes(INamedTypeSymbol type) =>
        type.GetTypeMembers().SelectMany(nested => DescendantTypes(nested).Prepend(nested));

    private static bool Inherits(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseType.OriginalDefinition))
                return true;
        }

        return false;
    }

    private static bool IsNumber(object value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
}
