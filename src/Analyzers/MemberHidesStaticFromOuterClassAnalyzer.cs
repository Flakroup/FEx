using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0004: a member of a nested type that has the name of a static member of a containing type.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MemberHidesStaticFromOuterClassAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0004";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Member hides static member from outer class",
        "'{0}' hides the static member '{1}' of the containing type",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "Inside the nested type a bare name binds to the nested member, so code written for the outer static member silently binds to the wrong one. Replaces the ReSharper inspection MemberHidesStaticFromOuterClass.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.Field, SymbolKind.Property, SymbolKind.Method, SymbolKind.Event, SymbolKind.NamedType);
    }

    private static void Analyze(SymbolAnalysisContext context)
    {
        var member = context.Symbol;
        if (!IsNamedDeclaration(member) || member.ContainingType.ContainingType is null)
            return;

        for (var outer = member.ContainingType.ContainingType; outer is not null; outer = outer.ContainingType)
        {
            foreach (var candidate in outer.GetMembers(member.Name))
            {
                if (!IsStaticMember(candidate) || !Clashes(member, candidate))
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(Rule, member.Locations[0], member, candidate));
                return;
            }
        }
    }

    // Overrides and enum members take their names from elsewhere; accessors, constructors, operators and indexers
    // are not named members, and a declaration outside the source has no place to report on.
    private static bool IsNamedDeclaration(ISymbol member) =>
        member is { IsImplicitlyDeclared: false, IsOverride: false, ContainingType.TypeKind: not TypeKind.Enum }
        && member.Locations.Length > 0
        && member.Locations[0].IsInSource
        && member is not IMethodSymbol { MethodKind: not MethodKind.Ordinary }
        && member is not IPropertySymbol { IsIndexer: true };

    // ReSharper reports every kind pairing by name, but two methods only clash when a call written for the outer one
    // would bind to the nested one: the same signature, return type aside.
    private static bool Clashes(ISymbol member, ISymbol candidate) =>
        member is not IMethodSymbol nested || candidate is not IMethodSymbol outer || HaveSameSignature(nested, outer);

    private static bool HaveSameSignature(IMethodSymbol nested, IMethodSymbol outer)
    {
        if (nested.TypeParameters.Length != outer.TypeParameters.Length || nested.Parameters.Length != outer.Parameters.Length)
            return false;

        var comparable = outer.IsGenericMethod ? outer.Construct(nested.TypeParameters.Cast<ITypeSymbol>().ToArray()) : outer;
        return !nested.Parameters
            .Where((parameter, index) => parameter.RefKind != comparable.Parameters[index].RefKind
                                         || !SymbolEqualityComparer.Default.Equals(parameter.Type, comparable.Parameters[index].Type))
            .Any();
    }

    private static bool IsStaticMember(ISymbol candidate) =>
        candidate is { IsStatic: true, IsImplicitlyDeclared: false }
        && candidate is IFieldSymbol or IPropertySymbol or IEventSymbol or IMethodSymbol { MethodKind: MethodKind.Ordinary };
}
