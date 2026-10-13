using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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
        "System.String", "System.Math", "System.MathF", "System.Char", "System.Object", "System.Array", "System.Type", "System.Nullable", "System.Linq.Enumerable",
    ];

    // 'string' and 'object' would print as keywords, not as the "System.String" the lists above spell.
    private static readonly SymbolDisplayFormat TypeName = SymbolDisplayFormat.CSharpErrorMessageFormat.RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static readonly string[] PureNamespaces = ["System.Collections", "System.Collections.Generic"];

    /// <summary>Reports the parameters of the member the blocks belong to, and of the lambdas and local functions inside, that the local (true) or global (false) rule covers.</summary>
    public static void Analyze(OperationBlockAnalysisContext context, DiagnosticDescriptor rule, bool local)
    {
        if (context.OwningSymbol is IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.Constructor or MethodKind.UserDefinedOperator or MethodKind.Conversion or MethodKind.ExplicitInterfaceImplementation } method
            && context.OperationBlocks.OfType<IBlockOperation>().FirstOrDefault(static block => block.Syntax is BlockSyntax { Parent: BaseMethodDeclarationSyntax }) is { } body
            && IsLocal(method) == local)
            Report(context, rule, method.Parameters, body, method.MethodKind == MethodKind.Constructor);

        // Nothing outside the member can call a lambda or a local function.
        if (!local)
            return;

        foreach (var function in context.OperationBlocks.SelectMany(static block => block.DescendantsAndSelf()))
        {
            switch (function)
            {
                case IAnonymousFunctionOperation lambda:
                    Report(context, rule, lambda.Symbol.Parameters, lambda.Body, true);
                    break;
                case ILocalFunctionOperation { Body: { } localBody } localFunction:
                    Report(context, rule, localFunction.Symbol.Parameters, localBody, false);
                    break;
            }
        }
    }

    // A constructor counts as local whoever can call it; an override or an interface implementation is reached through its base.
    private static bool IsLocal(IMethodSymbol method) =>
        method.MethodKind == MethodKind.Constructor
        || !method.IsOverride && !AnalyzerHelpers.ImplementsInterfaceMember(method) && AnalyzerHelpers.IsEffectivelyPrivate(method);

    private static void Report(OperationBlockAnalysisContext context, DiagnosticDescriptor rule, IEnumerable<IParameterSymbol> parameters, IBlockOperation body, bool checksAloneCount)
    {
        // An expression body has no statements to put checks at the top of.
        if (body.Syntax is not BlockSyntax)
            return;

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

        if (checks == 0 || checks == statements.Count && !checksAloneCount)
            return;

        var references = context.OperationBlocks
            .SelectMany(static block => block.DescendantsAndSelf())
            .OfType<IParameterReferenceOperation>()
            .Where(static reference => !AnalyzerHelpers.IsInsideNameOf(reference))
            .ToList();
        foreach (var parameter in parameters.Where(static parameter => parameter is { RefKind: not RefKind.Out, IsImplicitlyDeclared: false } && !parameter.Locations.IsEmpty))
        {
            var uses = references.Where(reference => SymbolEqualityComparer.Default.Equals(reference.Parameter, parameter)).ToList();
            if (uses.Count > 0 && uses.All(use => IsOnlyChecked(use, roots)))
                context.ReportDiagnostic(Diagnostic.Create(rule, parameter.Locations[0], parameter.Name));
        }
    }

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
                read = call.Arguments.Select(static argument => argument.Value).ToList();
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
            _ => method.IsStatic && method.Name.StartsWith("ThrowIf", StringComparison.Ordinal) && IsThrowHelperType(type),
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

            if (node.Parent is not { } parent || !PassesThrough(parent, node))
                return false;
        }
    }

    private static bool PassesThrough(IOperation parent, IOperation child) => parent switch
    {
        IBinaryOperation { OperatorMethod: var method } => method is null || IsReadOnlyMember(method),
        IUnaryOperation { OperatorMethod: var method } => method is null || IsReadOnlyMember(method),
        IConversionOperation { OperatorMethod: var method } => method is null || IsReadOnlyMember(method),
        IParenthesizedOperation or IIsPatternOperation or IIsTypeOperation or IConditionalOperation or ICoalesceOperation
            or IConditionalAccessOperation or IConditionalAccessInstanceOperation or IArrayElementReferenceOperation
            or IDelegateCreationOperation or IAnonymousFunctionOperation or IBlockOperation or IReturnOperation or IExpressionStatementOperation => true,
        IPropertyReferenceOperation reference => reference.Instance == child && IsReadOnlyMember(reference.Property),
        IFieldReferenceOperation reference => reference.Instance == child && IsReadOnlyMember(reference.Field),
        IArgumentOperation argument => argument.Parameter is { RefKind: RefKind.None or RefKind.In },
        IInvocationOperation call => IsReadOnlyMember(call.TargetMethod),
        _ => false,
    };

    // A member of the framework whose answer follows from its inputs; one of the user's own code could do anything with the parameter.
    private static bool IsReadOnlyMember(ISymbol member) =>
        member.Locations.All(static location => !location.IsInSource)
        && member.ContainingType is { } type
        && (PureTypes.Any(name => IsType(type, name)) || PureNamespaces.Contains(type.ContainingNamespace.ToDisplayString()));

    // 'List<T>' is "System.Collections.Generic.List<T>"; the name given leaves the type arguments out.
    private static bool IsType(INamedTypeSymbol type, string name)
    {
        var display = type.OriginalDefinition.ToDisplayString(TypeName);

        return display == name || display.StartsWith(name + "<", StringComparison.Ordinal);
    }
}
