using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0038: a local variable is named like a field or property of the enclosing type.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LocalVariableHidesMemberAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0038";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Local variable hides member",
        "Local variable '{0}' hides a field or property of the same name",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "Inside the scope the plain name means the local, so a read or write meant for the member silently goes to the local. Replaces the ReSharper inspection LocalVariableHidesMember.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            Analyze,
            SyntaxKind.VariableDeclarator,
            SyntaxKind.SingleVariableDesignation,
            SyntaxKind.ForEachStatement,
            SyntaxKind.CatchDeclaration);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var identifier = context.Node switch
        {
            VariableDeclaratorSyntax { Parent.Parent: not (FieldDeclarationSyntax or EventFieldDeclarationSyntax) } declarator => declarator.Identifier,
            SingleVariableDesignationSyntax designation => designation.Identifier,
            ForEachStatementSyntax loop => loop.Identifier,
            CatchDeclarationSyntax @catch => @catch.Identifier,
            _ => default,
        };

        // A field declarator and a catch without a variable declare no local.
        if (identifier.IsKind(SyntaxKind.None))
            return;

        if (context.SemanticModel.GetDeclaredSymbol(context.Node, context.CancellationToken) is { } local && MemberHiding.Hides(context.Compilation, local.ContainingSymbol, local.Name))
            context.ReportDiagnostic(Diagnostic.Create(Rule, identifier.GetLocation(), local.Name));
    }
}
