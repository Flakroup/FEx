using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0005: an overriding method or indexer whose last parameter drops the <c>params</c> of the overridden member.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BaseMemberHasParamsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0005";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Base member has params",
        "'{0}' overrides '{1}', whose last parameter is 'params', but does not declare it 'params' itself",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "A call through the derived type then needs an explicit array while the same call through the base type does not. Replaces the ReSharper inspection BaseMemberHasParams.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    // The override's own signature is shown without modifiers: its symbol would print the `params` it does not declare.
    private static readonly SymbolDisplayFormat OverrideFormat =
        SymbolDisplayFormat.CSharpErrorMessageFormat.RemoveParameterOptions(SymbolDisplayParameterOptions.IncludeParamsRefOut);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.Method, SymbolKind.Property);
    }

    private static void Analyze(SymbolAnalysisContext context)
    {
        switch (context.Symbol)
        {
            case IMethodSymbol { OverriddenMethod: { } baseMethod } method:
                Check(context, method, method.Parameters, baseMethod);
                break;
            case IPropertySymbol { OverriddenProperty: { } baseProperty } property:
                Check(context, property, property.Parameters, baseProperty);
                break;
        }
    }

    private static void Check(SymbolAnalysisContext context, ISymbol member, ImmutableArray<IParameterSymbol> parameters, ISymbol baseMember)
    {
        if (parameters.IsEmpty)
            return;

        var last = parameters.Length - 1;
        var parameter = parameters[last];
        var baseParameters = baseMember is IMethodSymbol baseMethod ? baseMethod.Parameters : ((IPropertySymbol)baseMember).Parameters;

        // The symbol of an override reports IsParams from the base member, so only the written modifier tells.
        if (baseParameters[last].IsParams
            && parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken) is ParameterSyntax syntax
            && !syntax.Modifiers.Any(SyntaxKind.ParamsKeyword))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, parameter.Locations[0], member.ToDisplayString(OverrideFormat), baseMember));
        }
    }
}
