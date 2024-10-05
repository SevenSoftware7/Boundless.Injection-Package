namespace SevenDev.Boundless.Injection.Generators;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public class InjectorMemberGenerator : IIncrementalGenerator {
	private static readonly string IInjector = "IInjector";
	private static readonly string GetInjectValue = "GetInjectValue";


	public void Initialize(IncrementalGeneratorInitializationContext context) {
		// Create a syntax provider to find class declarations with members having InjectableAttribute
		var classDeclarationsWithAttributes = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: (node, cancellationToken) => {
					return node is ClassDeclarationSyntax classDeclaration &&
						(classDeclaration.AttributeLists.SelectMany(attrList => attrList.Attributes).Any() ||
						classDeclaration.Members.Any(member => member.AttributeLists.SelectMany(attrList => attrList.Attributes).Any()));
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
									&& attributeSymbol.ToDisplayString() == InjectorAttribute.CachedType.FullName
								)
							))
						.Where(tuple => tuple.attribute != default)
						.ToArray();

					AttributeSyntax? classInjectorAttribute = classDeclaration.AttributeLists
						.SelectMany(attrList => attrList.Attributes)
						.FirstOrDefault(attribute =>
							semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol?.ContainingSymbol is INamedTypeSymbol attributeSymbol
							&& attributeSymbol.ToDisplayString() == InjectorAttribute.CachedType.FullName
						);

					return (classDeclaration, classInjectorAttribute, membersWithAttribute);
				}
			);


		// Combine the semantic model with the class declarations
		var compilationAndClasses = context.CompilationProvider
			.Combine(classDeclarationsWithAttributes.Collect());

		// Register the source generation output
		context.RegisterSourceOutput(compilationAndClasses, (spc, source) => {
			var (compilation, classWithMembers) = source;
			foreach (var (classDeclaration, classInjectorAttribute, membersInfo) in classWithMembers) {
				if (classDeclaration is null || membersInfo.Length == 0) continue;

				SemanticModel semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
				if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol) continue;


				if (!classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))) {
					spc.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InjectorClassMustBePartialDescriptor, classDeclaration.Identifier.GetLocation(), classSymbol.Name));
					continue;
				}


				Dictionary<ITypeSymbol, List<MemberWithAttributeData>> typeInjectors = [];

				foreach ((MemberDeclarationSyntax memberSyntax, AttributeSyntax attribute) in membersInfo) {
					TypeSyntax? typeSyntax = memberSyntax switch {
						MethodDeclarationSyntax methodDeclaration => GetMethodUniqueParameterType(methodDeclaration),
						PropertyDeclarationSyntax propertyDeclaration => GetPropertyType(propertyDeclaration),
						FieldDeclarationSyntax fieldDeclaration => GetFieldType(fieldDeclaration),
						_ => null,
					};
					if (typeSyntax is null || semanticModel.GetTypeInfo(typeSyntax).Type is not ITypeSymbol typeSymbol) continue;


					TypeSyntax? GetMethodUniqueParameterType(MethodDeclarationSyntax methodDeclaration) {
						if (methodDeclaration.ParameterList.Parameters.Count > 0) {
							spc.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.BadInjectorMethodParametersDescriptor, methodDeclaration.GetLocation(), classSymbol.Name));
							return null;
						}
						if (methodDeclaration.ReturnType.ToString() == "Void") {
							spc.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.VoidReturnTypeMethodDescriptor, methodDeclaration.GetLocation(), classSymbol.Name));
							return null;
						}
						return methodDeclaration.ReturnType;
					}
					TypeSyntax? GetPropertyType(PropertyDeclarationSyntax propertyDeclaration) {
						bool hasGetter = propertyDeclaration.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) ?? false;

						if (!hasGetter) {
							spc.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.GetterlessPropertyDescriptor, propertyDeclaration.GetLocation(), classSymbol.Name));
							return null;
						}
						return propertyDeclaration.Type;
					}
					TypeSyntax? GetFieldType(FieldDeclarationSyntax fieldDeclaration) {
						return fieldDeclaration.Declaration.Type;
					}

					ITypeSymbol nonNullableTypeSymbol = typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
					if (!typeInjectors.TryGetValue(nonNullableTypeSymbol, out var injectables)) {
						injectables = [];
						typeInjectors[nonNullableTypeSymbol] = injectables;
					}
					injectables.Add((memberSyntax, attribute));
				}

				if (classInjectorAttribute is not null) {
					ITypeSymbol nonNullableClassSymbol = classSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
					if (!typeInjectors.TryGetValue(nonNullableClassSymbol, out var injectables)) {
						injectables = [];
						typeInjectors[nonNullableClassSymbol] = injectables;
					}
					injectables.Add((classDeclaration, classInjectorAttribute));
				}

				if (typeInjectors.Count == 0) continue;


				Dictionary<ISymbol?, MemberDeclarationSyntax> typeUniqueInjectors = typeInjectors
					.Select<KeyValuePair<ITypeSymbol, List<MemberWithAttributeData>>, KeyValuePair<ITypeSymbol, MemberDeclarationSyntax>?>(typeInjector => {
						if (typeInjector.Value.Count > 1) {
							spc.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultipleInjectorsOfTypeDescriptor, classDeclaration.GetLocation(), classSymbol.Name));
							return null;
						}
						return new(typeInjector.Key, typeInjector.Value[0].MemberDeclaration);
					})
					.OfType<KeyValuePair<ITypeSymbol, MemberDeclarationSyntax>>()
					.ToDictionary(keySelector: pair => pair.Key, elementSelector: pair => pair.Value, SymbolEqualityComparer.Default);


				spc.AddSource($"{classSymbol}_Injector.generated.cs", GenerateCode(classSymbol, typeUniqueInjectors).ToString());
			}
		});
	}

	private static StringBuilder GenerateCode(INamedTypeSymbol classSymbol, Dictionary<ISymbol?, MemberDeclarationSyntax> typeInjectors) {
		StringBuilder codeBuilder = new();

		codeBuilder.AppendLine("using System;");
		codeBuilder.AppendLine("using SevenDev.Boundless.Injection;");
		codeBuilder.AppendLine();
		codeBuilder.AppendLine($"namespace {classSymbol.ContainingNamespace};");
		codeBuilder.AppendLine();
		codeBuilder.AppendLine($"public partial class {classSymbol.Name} : {string.Join(", ", typeInjectors.Keys.Select(typeSymbol => $"{IInjector}<{typeSymbol}>"))}");
		codeBuilder.AppendLine("{");

		foreach (var item in typeInjectors) {
			if (item.Key is not ITypeSymbol typeSymbol) continue;
			MemberDeclarationSyntax memberDeclaration = item.Value;

			codeBuilder.AppendLine($"    {typeSymbol} {IInjector}<{typeSymbol}>.{GetInjectValue}()");
			codeBuilder.AppendLine("    {");

			string memberInjectValueBody = memberDeclaration switch {
				ClassDeclarationSyntax classDeclaration => $"this",
				MethodDeclarationSyntax methodDeclaration => $"this.{methodDeclaration.Identifier.Text}()",
				PropertyDeclarationSyntax propertyDeclaration => $"this.{propertyDeclaration.Identifier.Text}",
				FieldDeclarationSyntax fieldDeclaration => $"this.{fieldDeclaration.Declaration.Variables[0].Identifier.Text}",
				_ => "default",
			};
			codeBuilder.AppendLine($"        return {memberInjectValueBody};");

			codeBuilder.AppendLine("    }");
		}
		codeBuilder.AppendLine("}");

		return codeBuilder;
	}
}
