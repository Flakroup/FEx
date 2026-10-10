using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0010: a <c>;</c> after the closing brace of a type or a block-scoped namespace.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantDeclarationSemicolonAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0010";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant semicolon after a declaration",
        "The semicolon after the closing brace is redundant",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "A type or namespace body is complete at its closing brace. An empty type body is left alone, as ReSharper does. Replaces the ReSharper inspection RedundantDeclarationSemicolon.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeType,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration,
            SyntaxKind.EnumDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeNamespace, SyntaxKind.NamespaceDeclaration);
    }

    private static void AnalyzeType(SyntaxNodeAnalysisContext context)
    {
        var type = (BaseTypeDeclarationSyntax)context.Node;
        if (type.SemicolonToken.IsKind(SyntaxKind.SemicolonToken) && !IsEmptyBody(type))
            Report(context, type.SemicolonToken);
    }

    private static void AnalyzeNamespace(SyntaxNodeAnalysisContext context)
    {
        var declaration = (NamespaceDeclarationSyntax)context.Node;
        if (declaration.SemicolonToken.IsKind(SyntaxKind.SemicolonToken))
            Report(context, declaration.SemicolonToken);
    }

    // "class X { };" is the one spelling ReSharper accepts: no members and nothing between the braces but whitespace.
    private static bool IsEmptyBody(BaseTypeDeclarationSyntax type) =>
        MemberCount(type) == 0
        && type.OpenBraceToken.TrailingTrivia.Concat(type.CloseBraceToken.LeadingTrivia)
            .All(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia) || trivia.IsKind(SyntaxKind.EndOfLineTrivia));

    // A base type declaration is either an enum or a class-like type, so the second arm is the enum.
    private static int MemberCount(BaseTypeDeclarationSyntax type) => type is TypeDeclarationSyntax declaration
        ? declaration.Members.Count
        : ((EnumDeclarationSyntax)type).Members.Count;

    private static void Report(SyntaxNodeAnalysisContext context, SyntaxToken semicolon) =>
        context.ReportDiagnostic(Diagnostic.Create(Rule, semicolon.GetLocation()));
}
