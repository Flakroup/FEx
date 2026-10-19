using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace FEx.Analyzers;

/// <summary>FEX0041: a method with optional parameters has an overload that takes only the leading parameters, which then wins for calls that leave the optional ones out.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MethodOverloadWithOptionalParameterAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0041";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Method with optional parameter is hidden by overload",
        "'{0}' is hidden by an overload without its optional parameter '{1}': a call that omits it binds to the overload",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "Overload resolution prefers the candidate that needs no default argument, so the optional parameter is never used by a call that leaves it out. Replaces the ReSharper inspection MethodOverloadWithOptionalParameter.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

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
        var overloads = method.ContainingType.GetMembers(method.Name).OfType<IMethodSymbol>()
            .Where(other => other.MethodKind == MethodKind.Ordinary && IsLeadingPart(other, method));
        foreach (var overload in overloads)
        {
            var optional = method.Parameters[overload.Parameters.Length];
            var syntax = optional.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken);
            context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.GetLocation(), method.Name, optional.Name));
        }
    }

    // The overload takes exactly the first parameters of the method, and every parameter the method declares after them is optional.
    private static bool IsLeadingPart(IMethodSymbol overload, IMethodSymbol method)
    {
        var count = overload.Parameters.Length;

        // An override is no new overload: a call binds to the method declared in the more derived class first.
        return !overload.IsOverride
               && count < method.Parameters.Length
               && overload.Arity == method.Arity
               && method.Parameters.Skip(count).All(parameter => parameter.IsOptional)
               && overload.Parameters.Zip(method.Parameters, (left, right) =>
                   left.RefKind == right.RefKind && AnalyzerHelpers.SameType(left.Type, right.Type)).All(same => same);
    }
}
