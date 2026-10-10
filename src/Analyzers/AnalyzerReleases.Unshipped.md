; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
FEX0001 | Correctness | Warning | NonAtomicCompoundOperatorAnalyzer
FEX0002 | Correctness | Warning | LoopVariableNeverChangedAnalyzer
FEX0003 | Correctness | Warning | VariableHidesOuterVariableAnalyzer
FEX0004 | Correctness | Warning | MemberHidesStaticFromOuterClassAnalyzer
FEX0005 | Correctness | Warning | BaseMemberHasParamsAnalyzer
FEX0006 | Correctness | Warning | RedundantVerbatimPrefixAnalyzer
FEX0007 | Correctness | Warning | RedundantVerbatimStringPrefixAnalyzer
FEX0008 | Correctness | Warning | RedundantLambdaSignatureParenthesesAnalyzer
FEX0009 | Correctness | Warning | RedundantAttributeParenthesesAnalyzer
FEX0010 | Correctness | Warning | RedundantDeclarationSemicolonAnalyzer
FEX0011 | Correctness | Warning | EmptyStatementAnalyzer
FEX0012 | Correctness | Warning | EmptyForStatementAnalyzer
FEX0013 | Correctness | Warning | EmptyNamespaceAnalyzer
FEX0014 | Correctness | Warning | RedundantCollectionInitializerElementBracesAnalyzer
FEX0015 | Correctness | Warning | RedundantQueryOrderByAscendingKeywordAnalyzer
FEX0016 | Correctness | Warning | RedundantEmptyFinallyBlockAnalyzer
FEX0017 | Correctness | Warning | RedundantEmptySwitchSectionAnalyzer
FEX0018 | Correctness | Warning | RedundantCaseLabelAnalyzer
FEX0019 | Correctness | Warning | RedundantJumpStatementAnalyzer
FEX0020 | Correctness | Warning | RedundantIfElseBlockAnalyzer
FEX0021 | Correctness | Warning | RedundantRangeBoundAnalyzer
FEX0022 | Redundancy | Warning | RedundantToStringCallAnalyzer
FEX0023 | Redundancy | Warning | RedundantToStringCallForValueTypeAnalyzer
FEX0024 | Redundancy | Warning | RedundantStringFormatCallAnalyzer
FEX0025 | Redundancy | Warning | RedundantStringInterpolationAnalyzer
FEX0026 | Redundancy | Warning | RedundantStringToCharArrayCallAnalyzer
FEX0027 | Redundancy | Warning | RedundantEnumerableCastCallAnalyzer
FEX0028 | Redundancy | Warning | RedundantDelegateCreationAnalyzer
FEX0029 | Redundancy | Warning | RedundantArgumentDefaultValueAnalyzer
FEX0030 | Redundancy | Warning | RedundantExplicitNullableCreationAnalyzer
FEX0031 | Redundancy | Warning | RedundantPropertyPatternClauseAnalyzer
FEX0032 | Redundancy | Warning | RedundantLogicalConditionalExpressionOperandAnalyzer
FEX0033 | Redundancy | Warning | RemoveRedundantOrStatementFalseAnalyzer
FEX0034 | Redundancy | Warning | RedundantCheckBeforeAssignmentAnalyzer
FEX0035 | Redundancy | Warning | RedundantCatchClauseAnalyzer
FEX0036 | Redundancy | Warning | RedundantBaseConstructorCallAnalyzer
FEX0037 | Correctness | Warning | ParameterHidesMemberAnalyzer
FEX0038 | Correctness | Warning | LocalVariableHidesMemberAnalyzer
FEX0039 | Correctness | Warning | RedundantOverloadGlobalAnalyzer
FEX0040 | Correctness | Warning | RedundantOverloadLocalAnalyzer
FEX0041 | Correctness | Warning | MethodOverloadWithOptionalParameterAnalyzer
FEX0042 | Correctness | Warning | OptionalParameterHierarchyMismatchAnalyzer
FEX0043 | Correctness | Warning | OptionalParameterRefOutAnalyzer
FEX0044 | Correctness | Warning | PartialMethodWithSinglePartAnalyzer
FEX0045 | Correctness | Warning | PartialTypeWithSinglePartAnalyzer
FEX0046 | Correctness | Warning | UnusedParameterInPartialMethodAnalyzer
FEX0047 | Correctness | Warning | RedundantOverriddenMemberAnalyzer
FEX0048 | Correctness | Warning | UnusedTypeParameterAnalyzer
FEX0049 | Correctness | Warning | UnusedMethodReturnValueLocalAnalyzer
FEX0050 | Correctness | Warning | ValueParameterNotUsedAnalyzer
FEX0051 | Correctness | Warning | ConvertToConstantLocalAnalyzer
FEX0052 | Correctness | Warning | MethodHasAsyncOverloadWithCancellationAnalyzer
FEX0053 | Correctness | Warning | UseCancellationTokenForIAsyncEnumerableAnalyzer
