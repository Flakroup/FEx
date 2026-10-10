using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0005: an override whose last parameter drops the <c>params</c> of the overridden method.</summary>
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

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.Method);
    }

    private static void Analyze(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.OverriddenMethod is not { Parameters.Length: > 0 } baseMethod || method.Parameters.Length != baseMethod.Parameters.Length)
            return;

        var last = method.Parameters.Length - 1;
        var parameter = method.Parameters[last];

        // The symbol of an override reports IsParams from the base method, so only the written modifier tells.
        if (baseMethod.Parameters[last].IsParams
            && parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken) is ParameterSyntax syntax
            && !syntax.Modifiers.Any(SyntaxKind.ParamsKeyword))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, parameter.Locations[0], method, baseMethod));
        }
    }
}
