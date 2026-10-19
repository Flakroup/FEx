using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Analyzers;

/// <summary>
/// What FEX0057 and FEX0058 share: a parameter whose every use sits in the precondition checks at the top of a body.
/// A check is <c>if (c) throw ...;</c>, <c>ThrowIfNull(x)</c> and its siblings, <c>Debug.Assert(c)</c>, <c>Contract.Requires(c)</c> or a call of
/// a method marked <c>[AssertionMethod]</c>; they count while they come before any other statement. A body of nothing but checks
/// is a validation method, and ReSharper leaves it alone, except in a constructor or a block lambda.
/// </summary>
internal static class PreconditionCheckAnalysis
{
    // The members whose answer follows from their arguments, so that reading a parameter through them is still only a check.
    private static readonly string[] PureTypes =
    [
        "System.String", "System.Math", "System.Char", "System.Object", "System.Array", "System.Type", "System.Nullable", "System.Linq.Enumerable",
    ];

    // 'string' and 'object' would print as keywords, not as the "System.String" the list above spells.
    private static readonly SymbolDisplayFormat TypeName = SymbolDisplayFormat.CSharpErrorMessageFormat.RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    // A collection answers questions with the members named here and changes with the rest: 'Add', 'Remove', 'Pop' and 'Clear' are uses.
    private static readonly string[] CollectionNamespaces = ["System.Collections", "System.Collections.Generic"];

    private static readonly string[] CollectionReads =
    [
        "Contains", "ContainsKey", "ContainsValue", "Count", "IndexOf", "LastIndexOf", "Exists", "TrueForAll", "Peek", "Keys", "Values",
    ];

    /// <summary>
    /// Starts FEX0057 (<paramref name="local"/> false) or FEX0058 (true). A method or a local function that some code takes as a method
    /// group or hooks up as an event handler is left out, because the delegate it converts to fixes its signature. Generated code (Razor
    /// binds handlers that way) is read for those uses and never reported.
    /// </summary>
    public static void Start(CompilationStartAnalysisContext start, DiagnosticDescriptor rule, bool local)
    {
        var candidates = new ConcurrentBag<(IMethodSymbol Method, Diagnostic Diagnostic)>();
        var handlers = new ConcurrentDictionary<IMethodSymbol, bool>(SymbolEqualityComparer.Default);
        start.RegisterOperationAction(operation => handlers[((IMethodReferenceOperation)operation.Operation).Method.OriginalDefinition] = true, OperationKind.MethodReference);
        start.RegisterOperationBlockAction(block => Analyze(block, rule, local, candidates));
        start.RegisterCompilationEndAction(end =>
        {
            foreach (var (_, diagnostic) in candidates.Where(candidate => !handlers.ContainsKey(candidate.Method)))
                end.ReportDiagnostic(diagnostic);
        });
    }

    // Looks at the member the blocks belong to, and at the lambdas and local functions inside, that the local (true) or global (false) rule covers.
    private static void Analyze(OperationBlockAnalysisContext context, DiagnosticDescriptor rule, bool local, ConcurrentBag<(IMethodSymbol, Diagnostic)> candidates)
    {
        if (context.OwningSymbol is not IMethodSymbol method)
            return;

        // Both are needed only once a body opens with a check.
        var generated = new Lazy<bool>(() => AnalyzerHelpers.IsGenerated(context.OperationBlocks[0].Syntax.SyntaxTree, method, context.Options, context.CancellationToken));
        var references = new Lazy<List<IParameterReferenceOperation>>(() => [.. context.OperationBlocks
            .SelectMany(static block => block.DescendantsAndSelf())
            .OfType<IParameterReferenceOperation>()
            .Where(static reference => !AnalyzerHelpers.IsInsideNameOf(reference))]);

        // Finding the checks is cheap and most bodies have none; deciding whether the member is covered walks the interfaces of its type.
        if (method.MethodKind is MethodKind.Ordinary or MethodKind.Constructor or MethodKind.UserDefinedOperator or MethodKind.Conversion
            && context.OperationBlocks.OfType<IBlockOperation>().FirstOrDefault() is { } body
            && FindChecks(body, method.MethodKind == MethodKind.Constructor) is { } roots
            && Covers(method, local)
            && !generated.Value)
        {
            foreach (var parameter in OnlyChecked(method.Parameters, roots, references.Value))
                candidates.Add((method, Diagnostic.Create(rule, parameter.Locations[0], parameter.Name)));
        }

        // Nothing outside the member can call a lambda or a local function.
        if (!local)
            return;

        foreach (var function in context.OperationBlocks.SelectMany(static block => block.DescendantsAndSelf()))
        {
            if (function is IAnonymousFunctionOperation lambda && FindChecks(lambda.Body, true) is { } lambdaRoots && !generated.Value)
            {
                foreach (var parameter in OnlyChecked(lambda.Symbol.Parameters, lambdaRoots, references.Value))
                    context.ReportDiagnostic(Diagnostic.Create(rule, parameter.Locations[0], parameter.Name));
            }
            else if (function is ILocalFunctionOperation { Body: { } localBody } localFunction && FindChecks(localBody, false) is { } localRoots && !generated.Value)
            {
                foreach (var parameter in OnlyChecked(localFunction.Symbol.Parameters, localRoots, references.Value))
                    candidates.Add((localFunction.Symbol, Diagnostic.Create(rule, parameter.Locations[0], parameter.Name)));
            }
        }
    }

    // A constructor counts as local whoever can call it. A virtual method, an override and an interface implementation are reached through their
    // base or their interface, which fix the signature; ReSharper reports those only at the root declaration, and only when every implementation just checks.
    private static bool Covers(IMethodSymbol method, bool local) =>
        method.MethodKind == MethodKind.Constructor
            ? local
            : !method.IsVirtual
              && !method.IsOverride
              && !AnalyzerHelpers.ImplementsInterfaceMember(method)
              && !AnalyzerHelpers.IsImplementedByDerivedType(method)
              && AnalyzerHelpers.IsEffectivelyPrivate(method) == local;

    // What the checks at the top of a body read, or null when the body has none or is nothing but checks (a validation method).
    private static List<IOperation>? FindChecks(IBlockOperation body, bool checksAloneCount)
    {
        // An expression body has no statements to put checks at the top of.
        if (body.Syntax is not BlockSyntax)
            return null;

        // A local function and a lambda end in a return the compiler adds; it is no statement of the body.
        var statements = body.Operations.Where(static statement => statement is not IReturnOperation { IsImplicit: true }).ToList();
        var roots = new List<IOperation>();
        var checks = 0;
        foreach (var statement in statements)
        {
            if (!TryGetCheck(statement, out var read))
                break;

            roots.AddRange(read);
            checks++;
        }

        // 'checks == 0' only saves collecting the references: with no check there is no root a use could sit in.
        return checks == 0 || checks == statements.Count && !checksAloneCount ? null : roots;
    }

    private static IEnumerable<IParameterSymbol> OnlyChecked(IEnumerable<IParameterSymbol> parameters, List<IOperation> roots, List<IParameterReferenceOperation> references) =>
        parameters.Where(parameter => references.Where(reference => SymbolEqualityComparer.Default.Equals(reference.Parameter, parameter)).ToList() is { Count: > 0 } uses
            && uses.All(use => IsOnlyChecked(use, roots)));

    // The operations a check reads: the condition of a guard, the arguments of a call.
    private static bool TryGetCheck(IOperation statement, out IReadOnlyList<IOperation> read)
    {
        read = [];
        switch (statement)
        {
            case IConditionalOperation { WhenFalse: null } guard when IsFailure(guard.WhenTrue):
                read = [guard.Condition];
                return true;
            case IExpressionStatementOperation { Operation: IInvocationOperation { TargetMethod: var method } call } when IsPreconditionCall(method):
                read = [.. call.Arguments.Select(static argument => argument.Value)];
                return true;
            default:
                return false;
        }
    }

    // 'throw ...;' or 'Debug.Fail(...);', alone or as the only statement of a block.
    private static bool IsFailure(IOperation action) => action switch
    {
        IBlockOperation { Operations: [var only] } => IsFailure(only),
        IThrowOperation => true,
        IExpressionStatementOperation { Operation: IInvocationOperation { TargetMethod: { Name: "Fail", ContainingType: var type } } } =>
            IsType(type, "System.Diagnostics.Debug") || IsType(type, "System.Diagnostics.Trace"),
        _ => false,
    };

    private static bool IsPreconditionCall(IMethodSymbol method)
    {
        if (method.GetAttributes().Any(static attribute => attribute.AttributeClass?.Name == "AssertionMethodAttribute"))
            return true;

        // A helper of the user's own code could do anything with its argument.
        if (method.Locations.Any(static location => location.IsInSource))
            return false;

        var type = method.ContainingType;

        return method.Name switch
        {
            "Assert" => IsType(type, "System.Diagnostics.Debug") || IsType(type, "System.Diagnostics.Trace") || IsType(type, "System.Diagnostics.Contracts.Contract"),
            "Requires" or "Assume" or "EndContractBlock" => IsType(type, "System.Diagnostics.Contracts.Contract"),
            _ => IsThrowHelperType(type),
        };
    }

    private static bool IsThrowHelperType(INamedTypeSymbol? type) =>
        type is not null && (IsType(type, "System.ArgumentException") || IsType(type, "System.ObjectDisposedException") || IsThrowHelperType(type.BaseType));

    // Climbs from a use to the root of a check, through the operators and members that only read.
    private static bool IsOnlyChecked(IOperation use, List<IOperation> roots)
    {
        for (var node = use; ; node = node.Parent!)
        {
            if (roots.Contains(node))
                return true;

            if (node.Parent is not { } parent || !PassesThrough(parent))
                return false;

            // In 'p?.Add(1)' the call runs on the access's instance, not on the operand the climb came from.
            if (parent is IConditionalAccessOperation access && access.Operation == node && !InstancesOf(access).All(instance => IsOnlyChecked(instance, roots)))
                return false;
        }
    }

    // The instances of the access, not those of an access nested in its right-hand side: that one asks about its own operand.
    private static IEnumerable<IConditionalAccessInstanceOperation> InstancesOf(IConditionalAccessOperation access) =>
        access.WhenNotNull.DescendantsAndSelf().OfType<IConditionalAccessInstanceOperation>().Where(instance => OwnerOf(instance) == access);

    // An instance sits in the right-hand side of exactly one access, so the walk up always ends at it.
    private static IConditionalAccessOperation OwnerOf(IConditionalAccessInstanceOperation instance)
    {
        for (var node = (IOperation)instance; ; node = node.Parent!)
        {
            if (node.Parent is IConditionalAccessOperation access && access.WhenNotNull == node)
                return access;
        }
    }

    // A pattern with a variable, a call with an 'out' or 'ref' argument, a field and a lambda carry the value on, to code the climb does not follow.
    private static bool PassesThrough(IOperation parent) => parent switch
    {
        IBinaryOperation or IUnaryOperation or IConversionOperation or IIsTypeOperation or IConditionalOperation or ICoalesceOperation
            or IConditionalAccessOperation or IArrayElementReferenceOperation => true,
        IIsPatternOperation pattern => !DeclaresVariable(pattern.Pattern),
        IPropertyReferenceOperation reference => IsReadOnlyMember(reference.Property),
        IArgumentOperation => true,
        IInvocationOperation call => IsReadOnlyMember(call.TargetMethod) && call.Arguments.All(static argument => argument.Parameter is not { RefKind: RefKind.Out }),
        _ => false,
    };

    // 'is not string s', 'is { Length: var n }' and 'is [_, .. var rest]' bind a variable the rest of the body can read.
    private static bool DeclaresVariable(IPatternOperation pattern) =>
        pattern.DescendantsAndSelf().Any(static operation => operation is IDeclarationPatternOperation { DeclaredSymbol: not null }
            or IRecursivePatternOperation { DeclaredSymbol: not null } or IListPatternOperation { DeclaredSymbol: not null });

    // A member of the framework whose answer follows from its inputs; one of the user's own code could do anything with the parameter.
    private static bool IsReadOnlyMember(ISymbol member) =>
        member.Locations.All(static location => !location.IsInSource)
        && member.ContainingType is { } type
        && (PureTypes.Any(name => IsType(type, name))
            || CollectionNamespaces.Contains(type.ContainingNamespace.ToDisplayString()) && (member is IPropertySymbol { IsIndexer: true } || CollectionReads.Contains(member.Name)));

    // 'List<T>' is "System.Collections.Generic.List<T>"; the name given leaves the type arguments out.
    private static bool IsType(INamedTypeSymbol type, string name)
    {
        var display = type.OriginalDefinition.ToDisplayString(TypeName);

        return display == name || display.StartsWith(name + "<", StringComparison.Ordinal);
    }
}
