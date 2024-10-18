using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SevenDev.Boundless.Injection.Generators {
	internal readonly struct InjectableMemberTypeData {
		internal readonly ITypeSymbol Symbol;
		internal readonly IEnumerable<InjectableMemberData> Members;


		public InjectableMemberTypeData(ITypeSymbol symbol, IEnumerable<InjectableMemberData> members) {
			Symbol = symbol;
			Members = members;
		}

		public static ResultOrDiagnostic<IEnumerable<InjectableMemberTypeData>> GetInjectableMemberTypeDataOrError(IEnumerable<(MemberDeclarationSyntax memberSyntax, AttributeSyntax attribute)> membersInfo, SemanticModel semanticModel) {
			Dictionary<ITypeSymbol, List<InjectableMemberData>> typeInjectables = new Dictionary<ITypeSymbol, List<InjectableMemberData>>(SymbolEqualityComparer.Default);


			foreach ((MemberDeclarationSyntax memberSyntax, AttributeSyntax attribute) in membersInfo) {
				ResultOrDiagnostic<TypeSyntax> memberTypeSyntax = GetMemberTypeSyntax(memberSyntax);
				if (memberTypeSyntax.HasDiagnostic) return memberTypeSyntax.Diagnostic;
				if (!memberTypeSyntax.HasResult) continue;
				if (!(semanticModel.GetTypeInfo(memberTypeSyntax.Result).Type is ITypeSymbol typeSymbol)) continue;

				SymbolInfo attributeSymbolInfo = semanticModel.GetSymbolInfo(attribute);

				int priority = 0;

				if (attributeSymbolInfo.Symbol?.ContainingType?.Name == InjectableAttribute.CachedType.Name) {
					ISymbol memberSymbol = semanticModel.GetDeclaredSymbol(memberSyntax);
					AttributeData attributeData = memberSymbol?.GetAttributes()
						.FirstOrDefault(a => a.AttributeClass?.Name == InjectableAttribute.CachedType.Name);

					priority = attributeData?.ConstructorArguments.FirstOrDefault().Value is int p ? p : 0;
				}

				ITypeSymbol nonNullableTypeSymbol = typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
				if (!typeInjectables.TryGetValue(nonNullableTypeSymbol, out var injectables)) {
					injectables = new List<InjectableMemberData>();
					typeInjectables[nonNullableTypeSymbol] = injectables;
				}
				injectables.Add((memberSyntax, attribute, priority));
			}

			return ResultOrDiagnostic<IEnumerable<InjectableMemberTypeData>>.FromResult(
				typeInjectables.Select(pair => new InjectableMemberTypeData(pair.Key, pair.Value))
			);
		}


		private static ResultOrDiagnostic<TypeSyntax> GetMemberTypeSyntax(MemberDeclarationSyntax memberSyntax) {
			switch(memberSyntax) {
				case MethodDeclarationSyntax methodDeclaration:
					if (methodDeclaration.ParameterList.Parameters.Count != 1) {
						return Diagnostic.Create(DiagnosticDescriptors.BadInjectMethodParametersDescriptor, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.Text);
					}

					return methodDeclaration.ParameterList.Parameters[0].Type;
				case PropertyDeclarationSyntax propertyDeclaration:
					bool hasSetter = propertyDeclaration.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false;

					if (!hasSetter) {
						return Diagnostic.Create(DiagnosticDescriptors.SetterlessPropertyDescriptor, propertyDeclaration.Identifier.GetLocation(), propertyDeclaration.Identifier.Text);
					}

					return propertyDeclaration.Type;
				case FieldDeclarationSyntax fieldDeclaration:
					bool isReadonly = fieldDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword));

					if (isReadonly) {
						VariableDeclaratorSyntax fieldDeclarator = fieldDeclaration.Declaration.Variables[0];
						return Diagnostic.Create(DiagnosticDescriptors.ReadonlyFieldDescriptor, fieldDeclarator.Identifier.GetLocation(), fieldDeclarator.Identifier.Text);
					}

					return fieldDeclaration.Declaration.Type;
				default:
					return default;
			}

		}
	}
}