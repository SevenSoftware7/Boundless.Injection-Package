using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SevenDev.Boundless.Injection.Generators {

	[Generator]
	public class InjectorMemberGenerator : IIncrementalGenerator {
		private static readonly string IInjector = "IInjector";
		private static readonly string GetInjectValue = "GetInjectValue";


		public void Initialize(IncrementalGeneratorInitializationContext initializationContext) {
			// Create a syntax provider to find class declarations with members having InjectableAttribute
			var classDeclarationsWithAttributes = initializationContext.SyntaxProvider.CreateSyntaxProvider(
				predicate: (node, cancellationToken) => {
					return node is ClassDeclarationSyntax classDeclaration &&
						(classDeclaration.AttributeLists.SelectMany(attrList => attrList.Attributes).Any() ||
						classDeclaration.Members.Any(member => member.AttributeLists.SelectMany(attrList => attrList.Attributes).Any()));
				},

				transform: (context, cancellationToken) => {
					ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;
					SemanticModel semanticModel = context.SemanticModel;

					(MemberDeclarationSyntax member, AttributeSyntax attribute)[] membersWithAttribute = classDeclaration.Members
						.Select(member => (member, attribute: member.AttributeLists.SelectOfType(InjectorAttribute.CachedType, semanticModel, cancellationToken)))
						.Where(tuple => tuple.attribute != default)
						.ToArray();

					AttributeSyntax classInjectorAttribute = classDeclaration.AttributeLists.SelectOfType(InjectorAttribute.CachedType, semanticModel, cancellationToken);

					return (classDeclaration, classInjectorAttribute, membersWithAttribute);
				}
			);


			// Combine the semantic model with the class declarations
			var compilationAndClasses = initializationContext.CompilationProvider
				.Combine(classDeclarationsWithAttributes.Collect());

			// Register the source generation output
			initializationContext.RegisterSourceOutput(compilationAndClasses, (spc, source) => {
				var (compilation, classWithMembers) = source;
				foreach (var (classDeclaration, classInjectorAttribute, membersInfo) in classWithMembers) {
					if (classDeclaration is null || membersInfo.Length == 0) continue;

					SemanticModel semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
					if (!(semanticModel.GetDeclaredSymbol(classDeclaration) is INamedTypeSymbol classSymbol)) continue;


					if (!classDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword))) {
						spc.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InjectorClassMustBePartialDescriptor, classDeclaration.Identifier.GetLocation(), classSymbol.Name));
						continue;
					}


					ResultOrDiagnostic<IEnumerable<InjectorMemberTypeData>> injectorMemberData = InjectorMemberTypeData.GetInjectorMemberTypeDataOrError(membersInfo, semanticModel, classSymbol, classDeclaration, classInjectorAttribute);
					if (injectorMemberData.HasDiagnostic) {
						spc.ReportDiagnostic(injectorMemberData.Diagnostic);
						continue;
					}
					if (!injectorMemberData.HasResult || injectorMemberData.Result.Count() == 0) continue;


					spc.AddSource($"{classSymbol}_Injector.generated.cs", GenerateCode(classSymbol, injectorMemberData.Result).ToString());
				}
			});
		}

		private static StringBuilder GenerateCode(INamedTypeSymbol classSymbol, IEnumerable<InjectorMemberTypeData> typeInjectors) {
			StringBuilder codeBuilder = new StringBuilder();

			IEnumerable<string> implementedInterfaces = typeInjectors.Select(data => data.Symbol)
				.Select(typeSymbol => $"{IInjector}<{typeSymbol.ToDisplayString()}>");
			string implementedInterfacesString = string.Join(", ", implementedInterfaces);

			codeBuilder.AppendLine("using System;");
			codeBuilder.AppendLine("using SevenDev.Boundless.Injection;");
			codeBuilder.AppendLine();
			codeBuilder.AppendLine($"namespace {classSymbol.ContainingNamespace}");
			codeBuilder.AppendLine("{");
			codeBuilder.AppendLine($"    public partial class {classSymbol.Name} : {implementedInterfacesString}");
			codeBuilder.AppendLine("    {");

			foreach (var item in typeInjectors) {
				MemberDeclarationSyntax memberDeclaration = item.Member.MemberDeclaration;

				codeBuilder.AppendLine($"        {item.Symbol} {IInjector}<{item.Symbol}>.{GetInjectValue}()");
				codeBuilder.AppendLine("        {");

				string memberInjectValueBody;
				switch (memberDeclaration) {
					case ClassDeclarationSyntax classDeclaration:
						memberInjectValueBody = "this";
						break;
					case MethodDeclarationSyntax methodDeclaration:
						memberInjectValueBody = $"this.{methodDeclaration.Identifier.Text}()";
						break;
					case PropertyDeclarationSyntax propertyDeclaration:
						memberInjectValueBody = $"this.{propertyDeclaration.Identifier.Text}";
						break;
					case FieldDeclarationSyntax fieldDeclaration:
						memberInjectValueBody = $"this.{fieldDeclaration.Declaration.Variables[0].Identifier.Text}";
						break;
					default:
						memberInjectValueBody = "default";
						break;
				}

				codeBuilder.AppendLine($"            return {memberInjectValueBody};");

				codeBuilder.AppendLine("        }");
			}
			codeBuilder.AppendLine("    }");
			codeBuilder.AppendLine("}");

			return codeBuilder;
		}
	}
}