using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0037: a parameter of a method with a body, or of a lambda, is named like a field or property of the enclosing type.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParameterHidesMemberAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0037";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Parameter hides member",
        "Parameter '{0}' hides a field or property of the same name",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "Inside the body the plain name means the parameter, so a read or write meant for the member silently goes to the parameter. Constructor parameters are exempt, as in ReSharper. Replaces the ReSharper inspection ParameterHidesMember.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Parameter);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var syntax = (ParameterSyntax)context.Node;
        if (MemberHiding.IsBodiless(syntax))
            return;

        var parameter = context.SemanticModel.GetDeclaredSymbol(syntax, context.CancellationToken)!;
        if (MemberHiding.Hides(context.Compilation, parameter.ContainingSymbol, parameter.Name))
            context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.Identifier.GetLocation(), parameter.Name));
    }
}
