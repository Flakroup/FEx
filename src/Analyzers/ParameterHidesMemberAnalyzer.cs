using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

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
        "Inside the body the plain name means the parameter, so a read or write meant for the member silently goes to the parameter. Constructor parameters are exempt, as in ReSharper. Replaces the ReSharper inspection ParameterHidesMember.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

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

        // '__arglist' is a parameter without a symbol.
        if (context.SemanticModel.GetDeclaredSymbol(syntax, context.CancellationToken) is not { } parameter || MemberHiding.IsPartOfOneDeclaration(parameter, context.CancellationToken))
            return;

        if (MemberHiding.Hides(context.Compilation, parameter.ContainingSymbol, parameter.Name))
            context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.Identifier.GetLocation(), parameter.Name));
    }
}
