; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
BD0001  |  Injectable  |  Error | MissingPartialModifierOnInjectableMemberClass
BD0002  |  Injectable  |  Error | InvalidInjectableMethodParameterCount
BD0003  |  Injectable  |  Error | MissingSetterInInjectableProperty
BD0004  |  Injectable  |  Error | ReadonlyInjectableField
BD0101  |  Injector  |  Error | MissingPartialModifierOnInjectorMemberClass
BD0102  |  Injector  |  Error | InvalidInjectorMethodParameterCount
BD0103  |  Injector  |  Error | InvalidInjectorMethodReturnType
BD0104  |  Injector  |  Error | MissingGetterInInjectorProperty
BD0105  |  Injector  |  Error | MultipleSameTypeInjectorsInClass