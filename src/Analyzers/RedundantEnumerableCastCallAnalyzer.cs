using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0027: <c>Cast&lt;T&gt;()</c> or <c>OfType&lt;T&gt;()</c> on a sequence whose elements already are exactly <c>T</c>.</summary>
/// <remarks>Identity only. ReSharper also reports a cast to a base type, which depends on how the result is used; this rule stays on the safe side of that.</remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantEnumerableCastCallAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0027";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant Enumerable cast call",
        "The call to '{0}<{1}>()' is redundant: the elements already are '{1}'",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "Cast<T> and OfType<T> on a sequence of T return its elements unchanged. Replaces the ReSharper inspection RedundantEnumerableCastCall.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var call = (InvocationExpressionSyntax)context.Node;

        // A null-conditional call returns null for a null source, which dropping the call would not.
        if (call.Expression is not MemberAccessExpressionSyntax access
            || context.SemanticModel.GetSymbolInfo(call).Symbol is not IMethodSymbol { Name: "Cast" or "OfType" } method
            || !SymbolEqualityComparer.Default.Equals(method.ContainingType, context.SemanticModel.Compilation.GetTypeByMetadataName("System.Linq.Enumerable")))
            return;

        var source = method.ReducedFrom is null
            ? call.ArgumentList.Arguments[0].Expression
            : access.Expression;
        if (ElementTypeOf(context.SemanticModel.GetTypeInfo(source).Type) is not { } element)
            return;

        // Annotations only matter where the call site reads them; a metadata type's nested annotations differ from oblivious source ones.
        var comparer = context.SemanticModel.GetNullableContext(call.SpanStart).AnnotationsEnabled()
            ? SymbolEqualityComparer.IncludeNullability
            : SymbolEqualityComparer.Default;
        if (comparer.Equals(element, method.TypeArguments[0]))
            context.ReportDiagnostic(Diagnostic.Create(Rule, access.Name.GetLocation(), method.Name, method.TypeArguments[0].ToDisplayString()));
    }

    /// <summary>The single <c>IEnumerable&lt;X&gt;</c> a type implements, or <c>null</c> for a non-generic sequence or an ambiguous one.</summary>
    private static ITypeSymbol? ElementTypeOf(ITypeSymbol? type)
    {
        if (type is IArrayTypeSymbol { Rank: 1 } array)
            return array.ElementType;

        // Only a named type is asked: a type parameter reports no interfaces of its own.
        var candidates = (type is INamedTypeSymbol named ? named.AllInterfaces.Add(named) : ImmutableArray<INamedTypeSymbol>.Empty)
            .Where(static i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            .ToList();
        return candidates.Count == 1 ? candidates[0].TypeArguments[0] : null;
    }
}
