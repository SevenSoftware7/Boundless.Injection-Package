namespace SevenDev.Boundless.Injection.Generators;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public class InjectableMemberGenerator : IIncrementalGenerator {
	private static readonly string IInjectable = "IInjectable";
	private static readonly string Inject = "Inject";


	public void Initialize(IncrementalGeneratorInitializationContext context) {
		// Create a syntax provider to find class declarations with members having InjectableAttribute
		var classDeclarationsWithAttributes = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: (node, cancellationToken) => {
					return node is ClassDeclarationSyntax classDeclaration && classDeclaration.Members.Any(member => member.AttributeLists.SelectMany(attrList => attrList.Attributes).Any());
				},

				transform: (context, cancellationToken) => {
					ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;
					SemanticModel semanticModel = context.SemanticModel;

					(MemberDeclarationSyntax member, AttributeSyntax attribute)[] membersWithAttribute = classDeclaration.Members
						.Select(member => (
							member,
							attribute: member.AttributeLists
								.SelectMany(attrList => attrList.Attributes)
								.FirstOrDefault(attribute =>
									semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol?.ContainingSymbol is INamedTypeSymbol attributeSymbol
									&& attributeSymbol.ToDisplayString() == InjectableAttribute.CachedType.FullName
								)
							))
						.Where(tuple => tuple.attribute != default)
						.ToArray();

					return (classDeclaration, membersWithAttribute);
				}
			);


		// Combine the semantic model with the class declarations
		var compilationAndClasses = context.CompilationProvider
			.Combine(classDeclarationsWithAttributes.Collect());

		// Register the source generation output
		context.RegisterSourceOutput(compilationAndClasses, (spc, source) => {
			var (compilation, classWithMembers) = source;
			foreach (var (classDeclaration, membersInfo) in classWithMembers) {
				if (classDeclaration is null || membersInfo.Length == 0) continue;

				SemanticModel semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);

				if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol) continue;

				if (!classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))) {
					spc.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InjectableClassMustBePartialDescriptor, classDeclaration.Identifier.GetLocation(), classSymbol.Name));
					continue;
				}


				Dictionary<ITypeSymbol, List<MemberWithAttributeData>> typeInjectables = [];

				foreach ((MemberDeclarationSyntax memberSyntax, AttributeSyntax attribute) in membersInfo) {
					TypeSyntax? typeSyntax = memberSyntax switch {
						MethodDeclarationSyntax methodDeclaration => GetMethodUniqueParameterType(methodDeclaration),
						PropertyDeclarationSyntax propertyDeclaration => GetPropertyType(propertyDeclaration),
						FieldDeclarationSyntax fieldDeclaration => GetFieldType(fieldDeclaration),
						_ => null,
					};
					if (typeSyntax is null || semanticModel.GetTypeInfo(typeSyntax).Type is not ITypeSymbol typeSymbol) continue;


					TypeSyntax? GetMethodUniqueParameterType(MethodDeclarationSyntax methodDeclaration) {
						if (methodDeclaration.ParameterList.Parameters.Count != 1) {
							spc.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.BadInjectMethodParametersDescriptor, methodDeclaration.Identifier.GetLocation(), classSymbol.Name));
							return null;
						}
						return methodDeclaration.ParameterList.Parameters[0].Type;
					}
					TypeSyntax? GetPropertyType(PropertyDeclarationSyntax propertyDeclaration) {
						bool hasSetter = propertyDeclaration.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false;

						if (!hasSetter) {
							spc.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SetterlessPropertyDescriptor, propertyDeclaration.Identifier.GetLocation(), classSymbol.Name));
							return null;
						}
						return propertyDeclaration.Type;
					}
					TypeSyntax? GetFieldType(FieldDeclarationSyntax fieldDeclaration) {
						bool isReadonly = fieldDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword));

						if (isReadonly) {
							spc.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ReadonlyFieldDescriptor, fieldDeclaration.Declaration.GetLocation(), classSymbol.Name));
							return null;
						}
						return fieldDeclaration.Declaration.Type;
					}

					ITypeSymbol nonNullableTypeSymbol = typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
					if (!typeInjectables.TryGetValue(nonNullableTypeSymbol, out var injectables)) {
						injectables = [];
						typeInjectables[nonNullableTypeSymbol] = injectables;
					}
					injectables.Add((memberSyntax, attribute));
				}
				if (typeInjectables.Count == 0) continue;

				Dictionary<ISymbol?, List<(MemberDeclarationSyntax memberDeclaration, int priority)>> typesWithPrioritizedMembers = typeInjectables
					.Select<KeyValuePair<ITypeSymbol, List<MemberWithAttributeData>>, KeyValuePair<ITypeSymbol, List<(MemberDeclarationSyntax memberDeclaration, int priority)>>>(typeInjector =>
						new(typeInjector.Key, typeInjector.Value
							.Select(memberWithAttribute => {
								SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberWithAttribute.Attribute);

								int priority = 0;

								if (symbolInfo.Symbol?.ContainingType?.Name == InjectableAttribute.CachedType.Name) {
									ISymbol? memberSymbol = semanticModel.GetDeclaredSymbol(memberWithAttribute.MemberDeclaration);
									AttributeData? attributeData = memberSymbol?.GetAttributes()
										.FirstOrDefault(a => a.AttributeClass?.Name == InjectableAttribute.CachedType.Name);

									priority = attributeData?.ConstructorArguments.FirstOrDefault().Value is int p ? p : 0;
								}

								return (memberWithAttribute.MemberDeclaration, priority);
							})
							.ToList()
						)
					)
					.ToDictionary(keySelector: pair => pair.Key, elementSelector: pair => pair.Value, SymbolEqualityComparer.Default);

				spc.AddSource($"{classSymbol}_Injectable.generated.cs", GenerateCode(classSymbol, typesWithPrioritizedMembers).ToString());
			}
		});
	}

	private static StringBuilder GenerateCode(INamedTypeSymbol classSymbol, Dictionary<ISymbol?, List<(MemberDeclarationSyntax memberDeclaration, int priority)>> typeInjectables) {
		StringBuilder codeBuilder = new();

		IEnumerable<string> implementedInterfaces = typeInjectables.Keys
			.OfType<ISymbol>()
			.Select(typeSymbol => $"{IInjectable}<{typeSymbol.ToDisplayString()}>");
		string implementedInterfacesString = string.Join(", ", implementedInterfaces);

		codeBuilder.AppendLine("using System;");
		codeBuilder.AppendLine("using SevenDev.Boundless.Injection;");
		codeBuilder.AppendLine();
		codeBuilder.AppendLine($"namespace {classSymbol.ContainingNamespace};");
		codeBuilder.AppendLine();
		codeBuilder.AppendLine($"public partial class {classSymbol.Name} : {implementedInterfacesString}");
		codeBuilder.AppendLine("{");

		foreach (var item in typeInjectables) {
			if (item.Key is not ITypeSymbol typeSymbol) continue;

			codeBuilder.AppendLine($"    void {IInjectable}<{typeSymbol}>.{Inject}({typeSymbol} value)");
			codeBuilder.AppendLine("    {");

			IEnumerable<MemberDeclarationSyntax> members = item.Value
				.OrderByDescending(member => member.priority)
				.Select(member => member.memberDeclaration);

			foreach (MemberDeclarationSyntax memberDeclaration in members) {
				string memberInjectBody = memberDeclaration switch {
					MethodDeclarationSyntax methodDeclaration => $"this.{methodDeclaration.Identifier.Text}(value)",
					PropertyDeclarationSyntax propertyDeclaration => $"this.{propertyDeclaration.Identifier.Text} = value",
					FieldDeclarationSyntax fieldDeclaration => $"this.{fieldDeclaration.Declaration.Variables[0].Identifier.Text} = value",
					_ => string.Empty,
				};
				codeBuilder.AppendLine($"        {memberInjectBody};");
			}

			codeBuilder.AppendLine("    }");
		}
		codeBuilder.AppendLine("}");

		return codeBuilder;
	}
}