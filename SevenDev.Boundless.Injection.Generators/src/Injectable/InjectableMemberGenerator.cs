using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SevenDev.Boundless.Injection.Generators {

	[Generator]
	public class InjectableMemberGenerator : IIncrementalGenerator {
		private static readonly string IInjectable = "IInjectable";
		private static readonly string Inject = "Inject";


		public void Initialize(IncrementalGeneratorInitializationContext initializationContext) {
			// Create a syntax provider to find class declarations with members having InjectableAttribute
			var classDeclarationsWithAttributes = initializationContext.SyntaxProvider.CreateSyntaxProvider(
				predicate: (node, cancellationToken) => {
					return node is ClassDeclarationSyntax classDeclaration && classDeclaration.Members.Any(member => member.AttributeLists.SelectMany(attrList => attrList.Attributes).Any());
				},

				transform: (context, cancellationToken) => {
					ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;
					SemanticModel semanticModel = context.SemanticModel;

					(MemberDeclarationSyntax member, AttributeSyntax attribute)[] membersWithAttribute = classDeclaration.Members
						.Select(member => (member, attribute: member.AttributeLists.SelectOfType(InjectableAttribute.CachedType, semanticModel, cancellationToken)))
						.Where(tuple => tuple.attribute != default)
						.ToArray();

					return (classDeclaration, membersWithAttribute);
				}
			);


			// Combine the semantic model with the class declarations
			var compilationAndClasses = initializationContext.CompilationProvider
				.Combine(classDeclarationsWithAttributes.Collect());

			// Register the source generation output
			initializationContext.RegisterSourceOutput(compilationAndClasses, (spc, source) => {
				var (compilation, classWithMembers) = source;
				foreach (var (classDeclaration, membersInfo) in classWithMembers) {
					if (classDeclaration is null || membersInfo.Length == 0) continue;

					SemanticModel semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);

					if (!(semanticModel.GetDeclaredSymbol(classDeclaration) is INamedTypeSymbol classSymbol)) continue;

					if (!classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))) {
						spc.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InjectableClassMustBePartialDescriptor, classDeclaration.Identifier.GetLocation(), classDeclaration.Identifier.Text));
						continue;
					}

					ResultOrDiagnostic<IEnumerable<InjectableMemberTypeData>> injectableMemberData = InjectableMemberTypeData.GetInjectableMemberTypeDataOrError(membersInfo, semanticModel);
					if (injectableMemberData.HasDiagnostic) {
						spc.ReportDiagnostic(injectableMemberData.Diagnostic);
						continue;
					}

					if (!injectableMemberData.HasResult || injectableMemberData.Result.Count() == 0) continue;

					spc.AddSource($"{classSymbol}_Injectable.generated.cs", GenerateCode(classSymbol, injectableMemberData.Result).ToString());
				}
			});
		}

		private static StringBuilder GenerateCode(INamedTypeSymbol classSymbol, IEnumerable<InjectableMemberTypeData> typeInjectables) {
			StringBuilder codeBuilder = new StringBuilder();

			IEnumerable<string> implementedInterfaces = typeInjectables.Select(data => data.Symbol)
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
				codeBuilder.AppendLine($"    void {IInjectable}<{item.Symbol}>.{Inject}({item.Symbol} @value)");
				codeBuilder.AppendLine("    {");

				IEnumerable<MemberDeclarationSyntax> members = item.Members
					.OrderByDescending(member => member.Priority)
					.Select(member => member.MemberDeclaration);

				foreach (MemberDeclarationSyntax memberDeclaration in members) {
					string memberInjectBody;
					switch (memberDeclaration) {
						case MethodDeclarationSyntax methodDeclaration:
							memberInjectBody = $"{methodDeclaration.Identifier.Text}(value)";
							break;
						case PropertyDeclarationSyntax propertyDeclaration:
							memberInjectBody = $"{propertyDeclaration.Identifier.Text} = value";
							break;
						case FieldDeclarationSyntax fieldDeclaration:
							memberInjectBody = $"{fieldDeclaration.Declaration.Variables[0].Identifier.Text} = value";
							break;
						default:
							memberInjectBody = "default";
							break;
					}
					codeBuilder.AppendLine($"        {memberInjectBody};");
				}

				codeBuilder.AppendLine("    }");
			}
			codeBuilder.AppendLine("}");

			return codeBuilder;
		}
	}
}