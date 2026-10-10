using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0047: an override whose body does nothing but call the same member of the base class with the same arguments.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantOverriddenMemberAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0047";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant overridden member",
        "Override '{0}' only calls the base implementation and can be removed",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "Removing it changes nothing, except where it is the second half of an Equals/GetHashCode pair that was written on purpose. Replaces the ReSharper inspection RedundantOverriddenMember.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration, SyntaxKind.PropertyDeclaration, SyntaxKind.IndexerDeclaration);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var member = (MemberDeclarationSyntax)context.Node;
        if (!IsPassThrough(member))
            return;

        var symbol = context.SemanticModel.GetDeclaredSymbol(member, context.CancellationToken)!;
        var overridden = symbol switch
        {
            IMethodSymbol method => method.OverriddenMethod,
            _ => (ISymbol?)((IPropertySymbol)symbol).OverriddenProperty,
        };

        if (overridden is null
            || !ParametersOf(symbol).Select(parameter => parameter.Name).SequenceEqual(ParametersOf(overridden).Select(parameter => parameter.Name))
            || HasWrittenCounterpart(symbol))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, IdentifierOf(member).GetLocation(), symbol.Name));
    }

    // Equals and GetHashCode go as a pair: a pass-through of one next to any hand-written other (an overload, a static
    // Equals, an IEquatable implementation) keeps the pair visible, as the contract between them is the reason both are there.
    private static bool HasWrittenCounterpart(ISymbol symbol)
    {
        var counterpart = symbol switch
        {
            IMethodSymbol { Name: "Equals" } => "GetHashCode",
            IMethodSymbol { Name: "GetHashCode" } => "Equals",
            _ => null,
        };

        return counterpart is not null
               && symbol.ContainingType.GetMembers(counterpart).OfType<IMethodSymbol>()
                   .SelectMany(other => other.DeclaringSyntaxReferences)
                   .Select(reference => reference.GetSyntax())
                   .OfType<MethodDeclarationSyntax>()
                   .Any(syntax => !IsPassThrough(syntax));
    }

    // A renamed parameter changes what a caller can write as a named argument, so the override is no longer a copy.
    private static ImmutableArray<IParameterSymbol> ParametersOf(ISymbol symbol) => symbol is IMethodSymbol method ? method.Parameters : ((IPropertySymbol)symbol).Parameters;

    private static SyntaxToken IdentifierOf(MemberDeclarationSyntax member) =>
        member switch
        {
            MethodDeclarationSyntax method => method.Identifier,
            PropertyDeclarationSyntax property => property.Identifier,
            _ => ((IndexerDeclarationSyntax)member).ThisKeyword,
        };

    /// <summary>
    /// Whether the declaration is a plain override whose every accessor or body consists of exactly one call of the
    /// base member of the same name with its own parameters.
    /// </summary>
    private static bool IsPassThrough(MemberDeclarationSyntax member) =>
        member.Modifiers.Any(SyntaxKind.OverrideKeyword)
        && !member.Modifiers.Any(SyntaxKind.SealedKeyword)
        && !member.Modifiers.Any(SyntaxKind.AsyncKeyword)
        && member.AttributeLists.Count == 0
        && member switch
        {
            MethodDeclarationSyntax method => IsCall(method),
            PropertyDeclarationSyntax property => AllBaseAccesses(property.ExpressionBody, property.AccessorList, property.Identifier.ValueText, null),
            _ => AllBaseAccesses(((IndexerDeclarationSyntax)member).ExpressionBody, ((IndexerDeclarationSyntax)member).AccessorList, null, ((IndexerDeclarationSyntax)member).ParameterList),
        };

    private static bool IsCall(MethodDeclarationSyntax method) =>
        SoleExpression(method.Body, method.ExpressionBody) is InvocationExpressionSyntax call
        && call.Expression is MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax, Name: var name }
        && name.Identifier.ValueText == method.Identifier.ValueText
        && ArgumentsPassThrough(call.ArgumentList.Arguments, method.ParameterList.Parameters);

    private static bool AllBaseAccesses(ArrowExpressionClauseSyntax? arrow, AccessorListSyntax? list, string? name, BracketedParameterListSyntax? indexer) =>
        arrow is not null
            ? IsBasePropertyAccess(arrow.Expression, name, indexer)
            : list!.Accessors.All(accessor => accessor.AttributeLists.Count == 0 && IsBasePropertyAccess(AccessTarget(accessor), name, indexer));

    // The base member a getter returns, or the one a setter or init accessor assigns `value` to.
    private static ExpressionSyntax? AccessTarget(AccessorDeclarationSyntax accessor)
    {
        var expression = SoleExpression(accessor.Body, accessor.ExpressionBody);

        return accessor.Keyword.IsKind(SyntaxKind.GetKeyword)
            ? expression
            : expression is AssignmentExpressionSyntax assignment
              && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
              && assignment.Right is IdentifierNameSyntax { Identifier.ValueText: "value" }
                ? assignment.Left
                : null;
    }

    private static bool IsBasePropertyAccess(ExpressionSyntax? expression, string? name, BracketedParameterListSyntax? indexer) =>
        indexer is null
            ? expression is MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax, Name: IdentifierNameSyntax property } && property.Identifier.ValueText == name
            : expression is ElementAccessExpressionSyntax { Expression: BaseExpressionSyntax } element
              && ArgumentsPassThrough(element.ArgumentList.Arguments, indexer.Parameters);

    // `=> e`, `{ e; }` or `{ return e; }`.
    private static ExpressionSyntax? SoleExpression(BlockSyntax? body, ArrowExpressionClauseSyntax? arrow) =>
        arrow?.Expression ?? body switch
        {
            { Statements: [ExpressionStatementSyntax statement] } => statement.Expression,
            { Statements: [ReturnStatementSyntax statement] } => statement.Expression,
            _ => null,
        };

    // The arguments are the parameters themselves, in order, passed the way the parameter is declared.
    private static bool ArgumentsPassThrough(SeparatedSyntaxList<ArgumentSyntax> arguments, SeparatedSyntaxList<ParameterSyntax> parameters) =>
        arguments.Count == parameters.Count
        && arguments.Zip(parameters, (argument, parameter) =>
        {
            var declared = parameter.Modifiers.FirstOrDefault(modifier => modifier.Kind() is SyntaxKind.RefKeyword or SyntaxKind.OutKeyword or SyntaxKind.InKeyword).Kind();

            return (argument.NameColon is null || argument.NameColon.Name.Identifier.ValueText == parameter.Identifier.ValueText)
                   && argument.Expression is IdentifierNameSyntax identifier
                   && identifier.Identifier.ValueText == parameter.Identifier.ValueText
                   && argument.RefKindKeyword.IsKind(declared);
        }).All(matches => matches);
}
