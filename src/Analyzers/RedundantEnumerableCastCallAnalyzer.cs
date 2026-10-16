using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0027: <c>Cast&lt;T&gt;()</c> on a sequence whose elements already are exactly <c>T</c>.</summary>
/// <remarks>
/// Identity only. ReSharper also reports a cast to a base type, which depends on how the result is used, and <c>OfType&lt;T&gt;()</c>, which drops the nulls;
/// this rule stays on the safe side of both. A <c>Cast</c> is reported where the sequence keeps the static type it would have without the call.
/// </remarks>
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
        "Cast<T> on a sequence of T returns its elements unchanged. Replaces the ReSharper inspection RedundantEnumerableCastCall.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

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
        var model = context.SemanticModel;

        // A null-conditional call returns null for a null source, which dropping the call would not.
        if (call.Expression is not MemberAccessExpressionSyntax access
            || model.GetSymbolInfo(call, context.CancellationToken).Symbol is not IMethodSymbol { Name: "Cast" } method
            || !SymbolEqualityComparer.Default.Equals(method.ContainingType, model.Compilation.GetTypeByMetadataName("System.Linq.Enumerable")))
            return;

        var source = method.ReducedFrom is null
            ? call.ArgumentList.Arguments[0].Expression
            : access.Expression;
        var sourceType = model.GetTypeInfo(source, context.CancellationToken).Type;
        if (ElementTypeOf(sourceType) is not { } element)
            return;

        // Annotations only matter where the call site reads them; a metadata type's nested annotations differ from oblivious source ones.
        var comparer = model.GetNullableContext(call.SpanStart).AnnotationsEnabled()
            ? SymbolEqualityComparer.IncludeNullability
            : SymbolEqualityComparer.Default;
        if (comparer.Equals(element, method.TypeArguments[0]) && KeepsMeaning(model, call, source, comparer.Equals(sourceType, method.ReturnType), context.CancellationToken))
            context.ReportDiagnostic(Diagnostic.Create(Rule, access.Name.GetLocation(), method.Name, method.TypeArguments[0].ToDisplayString()));
    }

    /// <summary>
    /// Whether the code around <paramref name="call"/> reads the same with the call gone. Cast returns <c>IEnumerable&lt;T&gt;</c> whatever it was given,
    /// so a <c>List&lt;T&gt;</c> or an array turns up as itself in an overload choice, a <c>var</c> or an inferred type argument.
    /// </summary>
    private static bool KeepsMeaning(SemanticModel model, InvocationExpressionSyntax call, ExpressionSyntax source, bool sameType, CancellationToken cancellationToken)
    {
        if (sameType)
            return true;

        SyntaxNode node = call;
        while (node.Parent is ParenthesizedExpressionSyntax)
            node = node.Parent;

        return node.Parent switch
        {
            ArgumentSyntax argument => Speculation.OwnerOf(argument) is { } owner
                && Speculation.BindsToSame(model, owner, owner.ReplaceNode(call, source.WithTriviaFrom(call)), cancellationToken),
            MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax invocation } access when access.Expression == node =>
                Speculation.BindsToSame(model, invocation, invocation.ReplaceNode(call, source.WithTriviaFrom(call)), cancellationToken),
            // Where the target has a type of its own, nothing is inferred from the sequence.
            ForEachStatementSyntax or ArrowExpressionClauseSyntax => true,
            ReturnStatementSyntax returned => returned.Ancestors().FirstOrDefault(static a => a is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or BaseMethodDeclarationSyntax or AccessorDeclarationSyntax)
                is not AnonymousFunctionExpressionSyntax,
            EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type.IsVar: false } } } => true,
            AssignmentExpressionSyntax assignment => assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) && assignment.Right == node,
            _ => false,
        };
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
