; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
BD0000  | Injectable | Error | MissingPartialModifierOnInjectableMemberClass
BD0001  | Injectable | Error | InvalidInjectableMethodParameterCount
BD0002  | Injectable | Error | MissingSetterInInjectableProperty
BD0003  | Injectable | Error | ReadonlyInjectableField
BD0100  | Injector   | Error | MissingPartialModifierOnInjectorMemberClass
BD0101  | Injector   | Error | InvalidInjectorMethodParameterCount
BD0102  | Injector   | Error | InvalidInjectorMethodReturnType
BD0103  | Injector   | Error | MissingGetterInInjectorProperty
BD0104  | Injector   | Error | MultipleSameTypeInjectorsInClass