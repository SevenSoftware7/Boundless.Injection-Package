using Microsoft.CodeAnalysis;

namespace Seven.Boundless.Injection.Generators {

	public static class DiagnosticDescriptors {
		public static readonly DiagnosticDescriptor InjectableClassMustBePartialDescriptor = new DiagnosticDescriptor(
			id: "BD0001",
			title: "Injectable member in non-partial class",
			messageFormat: $"Class '{{0}}' with member(s) marked with [{Utility.InjectableAttributeTypeFullName}] must be partial",
			category: DiagnosticCategories.Injectable,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);
		public static readonly DiagnosticDescriptor BadInjectMethodParametersDescriptor = new DiagnosticDescriptor(
			id: "BD0002",
			title: "Invalid parameters for Injectable method",
			messageFormat: $"Method '{{0}}' marked with [{Utility.InjectableAttributeTypeFullName}] must have exactly one parameter",
			category: DiagnosticCategories.Injectable,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);
		public static readonly DiagnosticDescriptor SetterlessPropertyDescriptor = new DiagnosticDescriptor(
			id: "BD0003",
			title: "No setter in Injectable property",
			messageFormat: $"Property '{{0}}' marked with [{Utility.InjectableAttributeTypeFullName}] must have a setter",
			category: DiagnosticCategories.Injectable,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);
		public static readonly DiagnosticDescriptor ReadonlyFieldDescriptor = new DiagnosticDescriptor(
			id: "BD0004",
			title: "Injectable field is read-only",
			messageFormat: $"Field '{{0}}' marked with [{Utility.InjectableAttributeTypeFullName}] must not be read-only",
			category: DiagnosticCategories.Injectable,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public static readonly DiagnosticDescriptor InjectorClassMustBePartialDescriptor = new DiagnosticDescriptor(
			id: "BD0101",
			title: "Injector member in non-partial class",
			messageFormat: $"Class '{{0}}' with member(s) marked with [{Utility.InjectorAttributeTypeFullName}] must be partial",
			category: DiagnosticCategories.Injector,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);
		public static readonly DiagnosticDescriptor BadInjectorMethodParametersDescriptor = new DiagnosticDescriptor(
			id: "BD0102",
			title: "Unwanted parameters for Injector method",
			messageFormat: $"Method '{{0}}' marked with [{Utility.InjectorAttributeTypeFullName}] must be parameterless",
			category: DiagnosticCategories.Injector,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);
		public static readonly DiagnosticDescriptor VoidReturnTypeMethodDescriptor = new DiagnosticDescriptor(
			id: "BD0103",
			title: "Void return type for Injector method",
			messageFormat: $"Method '{{0}}' marked with [{Utility.InjectorAttributeTypeFullName}] must not have void return type",
			category: DiagnosticCategories.Injector,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);
		public static readonly DiagnosticDescriptor GetterlessPropertyDescriptor = new DiagnosticDescriptor(
			id: "BD0104",
			title: "No getter in Injector property",
			messageFormat: $"Property '{{0}}' marked with [{Utility.InjectorAttributeTypeFullName}] must have a getter",
			category: DiagnosticCategories.Injector,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);
		public static readonly DiagnosticDescriptor MultipleInjectorsOfTypeDescriptor = new DiagnosticDescriptor(
			id: "BD0105",
			title: "Multiple Injectors of the same type",
			messageFormat: $"Class '{{0}}' must not have multiple members with the same return type marked with [{Utility.InjectorAttributeTypeFullName}]",
			category: DiagnosticCategories.Injector,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);
	}
}