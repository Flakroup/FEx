using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0029: a trailing positional argument that spells out the default value of an optional parameter.</summary>
/// <remarks>Reported only when the call, with every such argument gone, still binds to the same member.</remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantArgumentDefaultValueAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0029";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant argument default value",
        "The argument is redundant: it equals the default value of its parameter",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "A trailing optional argument that repeats the parameter's default can be left out. Replaces the ReSharper inspection RedundantArgumentDefaultValue.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            Analyze,
            SyntaxKind.InvocationExpression,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.ImplicitObjectCreationExpression,
            SyntaxKind.ElementAccessExpression,
            SyntaxKind.BaseConstructorInitializer,
            SyntaxKind.ThisConstructorInitializer,
            SyntaxKind.Attribute);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var node = context.Node;
        var model = context.SemanticModel;

        var arguments = ArgumentsOf(node);
        if (arguments.IsDefaultOrEmpty
            || ParametersOf(model.GetSymbolInfo(node, context.CancellationToken).Symbol) is not { } parameters
            || IsInExpressionTree(node, model, context.CancellationToken))
            return;

        var redundant = RedundantArguments(arguments, parameters, model, context.CancellationToken);
        if (redundant.Count == 0)
            return;

        if (!Speculation.BindsToSame(model, node, Without(node, redundant), context.CancellationToken))
            return;

        foreach (var argument in redundant)
            context.ReportDiagnostic(Diagnostic.Create(Rule, argument.Node.GetLocation()));
    }

    /// <summary>One argument of a call: the node that goes away with it, the value it passes and whether it stands by position.</summary>
    private readonly struct Argument
    {
        public Argument(SyntaxNode node, ExpressionSyntax expression, bool positional)
        {
            Node = node;
            Expression = expression;
            Positional = positional;
        }

        public SyntaxNode Node { get; }

        public ExpressionSyntax Expression { get; }

        public bool Positional { get; }
    }

    private static ImmutableArray<Argument> ArgumentsOf(SyntaxNode node) => node switch
    {
        InvocationExpressionSyntax call => FromList(call.ArgumentList),
        BaseObjectCreationExpressionSyntax { ArgumentList: { } list } => FromList(list),
        ElementAccessExpressionSyntax access => FromList(access.ArgumentList),
        ConstructorInitializerSyntax initializer => FromList(initializer.ArgumentList),
        AttributeSyntax { ArgumentList: { } list } => list.Arguments
            .Select(static a => new Argument(a, a.Expression, a.NameColon is null && a.NameEquals is null))
            .ToImmutableArray(),
        _ => default,
    };

    private static ImmutableArray<Argument> FromList(BaseArgumentListSyntax list) => list.Arguments
        .Select(static a => new Argument(a, a.Expression, a.NameColon is null))
        .ToImmutableArray();

    private static ImmutableArray<IParameterSymbol>? ParametersOf(ISymbol? symbol) => symbol switch
    {
        IMethodSymbol method => method.Parameters,
        IPropertySymbol { IsIndexer: true } indexer => indexer.Parameters,
        _ => null,
    };

    private static List<Argument> RedundantArguments(
        ImmutableArray<Argument> arguments,
        ImmutableArray<IParameterSymbol> parameters,
        SemanticModel model,
        CancellationToken cancellationToken)
    {
        var redundant = new List<Argument>();

        // An argument stands in the parameter's own position only while nothing before it was named.
        var positionalPrefix = arguments.TakeWhile(static a => a.Positional).Count();

        // From the right: an argument can go only if every positional argument after it can go too.
        var laterPositionalStay = false;
        for (var i = arguments.Length - 1; i >= 0; i--)
        {
            if (!arguments[i].Positional)
                continue;

            if (!laterPositionalStay && i < positionalPrefix && i < parameters.Length && RepeatsDefault(arguments[i].Expression, parameters[i], model, cancellationToken))
                redundant.Add(arguments[i]);
            else
                laterPositionalStay = true;
        }

        return redundant;
    }

    private static bool RepeatsDefault(ExpressionSyntax expression, IParameterSymbol parameter, SemanticModel model, CancellationToken cancellationToken)
    {
        if (!parameter.HasExplicitDefaultValue
            || parameter.GetAttributes().Any(static a => a.AttributeClass?.Name is "CallerMemberNameAttribute" or "CallerFilePathAttribute" or "CallerLineNumberAttribute" or "CallerArgumentExpressionAttribute"))
            return false;

        var constant = model.GetConstantValue(expression, cancellationToken);
        var defaultValue = parameter.ExplicitDefaultValue;

        if (constant.HasValue)
            return ConstantsEqual(constant.Value, defaultValue);

        // A struct's default has no constant form: 'default', 'default(S)' and 'new S()' all spell it.
        return defaultValue is null
            && SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(expression, cancellationToken).Type, parameter.Type)
            && SpellsDefault(expression, model, cancellationToken);
    }

    // 'new S()' is the default only while no constructor was declared: a C# 10 struct may declare its own parameterless one,
    // and 'new T()' for a type parameter runs whichever one the argument has.
    private static bool SpellsDefault(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken) => expression switch
    {
        DefaultExpressionSyntax or LiteralExpressionSyntax { RawKind: (int)SyntaxKind.DefaultLiteralExpression } => true,
        ObjectCreationExpressionSyntax { Initializer: null } creation =>
            model.GetSymbolInfo(creation, cancellationToken).Symbol is not { IsImplicitlyDeclared: false }
            && model.GetTypeInfo(creation, cancellationToken).Type is not ITypeParameterSymbol,
        _ => false,
    };

    // Roslyn hands constants over as boxed primitives; an int literal for a double or long parameter is the same default.
    // Exactly the same value, though: '-0.0' is not '0.0' (1 / x tells them apart) and '1.0m' prints differently from '1m'.
    private static bool ConstantsEqual(object? left, object? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        if (left is string || right is string || left is char || right is char)
            return left.Equals(right);

        return left is float or double || right is float or double
            ? BitConverter.DoubleToInt64Bits(Convert.ToDouble(left)) == BitConverter.DoubleToInt64Bits(Convert.ToDouble(right))
            : decimal.GetBits(Convert.ToDecimal(left)).SequenceEqual(decimal.GetBits(Convert.ToDecimal(right)));
    }

    private static SyntaxNode Without(SyntaxNode node, List<Argument> redundant)
    {
        var gone = redundant.Select(static a => a.Node).ToList();
        return node switch
        {
            InvocationExpressionSyntax call => call.WithArgumentList(call.ArgumentList.WithArguments(Keep(call.ArgumentList.Arguments, gone))),
            ObjectCreationExpressionSyntax creation => creation.WithArgumentList(creation.ArgumentList!.WithArguments(Keep(creation.ArgumentList.Arguments, gone))),
            ImplicitObjectCreationExpressionSyntax creation => creation.WithArgumentList(creation.ArgumentList.WithArguments(Keep(creation.ArgumentList.Arguments, gone))),
            ElementAccessExpressionSyntax access => access.WithArgumentList(access.ArgumentList.WithArguments(Keep(access.ArgumentList.Arguments, gone))),
            ConstructorInitializerSyntax initializer => initializer.WithArgumentList(initializer.ArgumentList.WithArguments(Keep(initializer.ArgumentList.Arguments, gone))),
            _ => WithoutAttributeArguments((AttributeSyntax)node, gone),
        };
    }

    private static AttributeSyntax WithoutAttributeArguments(AttributeSyntax attribute, List<SyntaxNode> gone) =>
        attribute.WithArgumentList(attribute.ArgumentList!.WithArguments(Keep(attribute.ArgumentList.Arguments, gone)));

    private static SeparatedSyntaxList<T> Keep<T>(SeparatedSyntaxList<T> arguments, List<SyntaxNode> gone)
        where T : SyntaxNode =>
        SyntaxFactory.SeparatedList(arguments.Where(a => !gone.Contains(a)));

    // An expression tree cannot contain a call that leaves an optional argument out.
    private static bool IsInExpressionTree(SyntaxNode node, SemanticModel model, CancellationToken cancellationToken)
    {
        var compilation = model.Compilation;
        var expression = compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");
        var queryable = compilation.GetTypeByMetadataName("System.Linq.IQueryable");

        foreach (var ancestor in node.Ancestors())
        {
            var isTree = ancestor switch
            {
                LambdaExpressionSyntax lambda =>
                    SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(lambda, cancellationToken).ConvertedType?.OriginalDefinition, expression),
                // A query over an IQueryable is translated into lambdas that become expression trees.
                QueryExpressionSyntax query => IsQueryable(model.GetTypeInfo(query, cancellationToken).Type),
                _ => false,
            };
            if (isTree)
                return true;
        }

        return false;

        bool IsQueryable(ITypeSymbol? type) => type is not null
            && (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, queryable)
                || type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, queryable)));
    }
}
