using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Analyzers;

/// <summary>What FEX0054 and FEX0055 share: whether a pair of empty brackets holds anything but whitespace.</summary>
internal static class EmptyInitializers
{
    public static bool HasComment(IEnumerable<SyntaxTrivia> trivia) =>
        trivia.Any(static item => !item.IsKind(SyntaxKind.WhitespaceTrivia) && !item.IsKind(SyntaxKind.EndOfLineTrivia));
}
