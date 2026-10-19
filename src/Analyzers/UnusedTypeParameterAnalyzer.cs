using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace FEx.Analyzers;

/// <summary>FEX0048: a type parameter of a method, local function, type or delegate that nothing mentions.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnusedTypeParameterAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0048";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Unused type parameter",
        "Type parameter '{0}' is never used",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "Callers have to spell out or infer an argument that changes nothing. A type parameter an interface, a base class or a documentation comment mentions counts as used. Replaces the ReSharper inspection UnusedTypeParameter.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.TypeParameter);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var parameter = (TypeParameterSyntax)context.Node;
        var declaration = parameter.Parent!.Parent!;
        var name = parameter.Identifier.ValueText;
        if (IsFixedBySignature(context, declaration))
            return;

        var scopes = declaration is TypeDeclarationSyntax
            ? context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken)!.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax(context.CancellationToken)).ToList()
            : [declaration];
        // A static field exists once per closed type, so dropping the parameter merges the copies the callers rely on.
        if (declaration is TypeDeclarationSyntax && scopes.Any(HasStaticStorage))
            return;

        if (!scopes.Any(scope => IsMentioned(scope, name)))
            context.ReportDiagnostic(Diagnostic.Create(Rule, parameter.Identifier.GetLocation(), name));
    }

    // What an abstract, virtual or overriding method, an interface member (default bodies included) or an implementation of one has no say over:
    // its type parameters are part of a contract written elsewhere. A declaration without a body is the contract itself.
    private static bool IsFixedBySignature(SyntaxNodeAnalysisContext context, SyntaxNode declaration)
    {
        if (declaration is not MethodDeclarationSyntax method)
            return false;

        if (method.Body is null && method.ExpressionBody is null && !method.Modifiers.Any(SyntaxKind.ExternKeyword)
            || method.Modifiers.Any(SyntaxKind.VirtualKeyword)
            || method.Modifiers.Any(SyntaxKind.OverrideKeyword)
            || method.Parent is InterfaceDeclarationSyntax)
        {
            return true;
        }

        var symbol = context.SemanticModel.GetDeclaredSymbol(method, context.CancellationToken)!;

        return AnalyzerHelpers.ImplementsInterfaceMember(symbol);
    }

    private static bool HasStaticStorage(SyntaxNode scope) =>
        scope.DescendantNodes().Any(node => node switch
        {
            FieldDeclarationSyntax or EventFieldDeclarationSyntax => ((MemberDeclarationSyntax)node).Modifiers.Any(SyntaxKind.StaticKeyword),
            PropertyDeclarationSyntax property => property.Modifiers.Any(SyntaxKind.StaticKeyword) && property.AccessorList is { } list && list.Accessors.Any(accessor => accessor is { Body: null, ExpressionBody: null }),
            _ => false,
        });

    private static bool IsMentioned(SyntaxNode scope, string name)
    {
        // A nested declaration with a type parameter of the same name hides this one for everything inside it.
        var names = scope
            .DescendantNodes(node => node == scope || !DeclaresTypeParameter(node, name))
            .OfType<IdentifierNameSyntax>()
            .Any(identifier => identifier.Identifier.ValueText == name && identifier.Parent is not TypeParameterConstraintClauseSyntax);

        return names || scope.GetLeadingTrivia().Any(trivia => IsDocumentationOf(trivia, name));
    }

    private static bool DeclaresTypeParameter(SyntaxNode node, string name) =>
        node.ChildNodes().OfType<TypeParameterListSyntax>().Any(list => list.Parameters.Any(parameter => parameter.Identifier.ValueText == name));

    // The text, not the parsed tag: the comment is a plain comment unless the project parses documentation.
    private static bool IsDocumentationOf(SyntaxTrivia trivia, string name)
    {
        var text = trivia.ToFullString();

        return (text.StartsWith("///", StringComparison.Ordinal) || text.StartsWith("/**", StringComparison.Ordinal))
               && (text.Contains($"<typeparam name=\"{name}\"") || text.Contains($"<typeparamref name=\"{name}\""));
    }
}
