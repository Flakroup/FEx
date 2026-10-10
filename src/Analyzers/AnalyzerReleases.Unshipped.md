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
