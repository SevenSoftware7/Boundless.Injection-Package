using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SevenDev.Boundless.Injection.Generators {
	internal readonly struct InjectorMemberTypeData {
		internal readonly ITypeSymbol Symbol;
		internal readonly InjectorMemberData Member;


		public InjectorMemberTypeData(ITypeSymbol symbol, InjectorMemberData member) {
			Symbol = symbol;
			Member = member;
		}

		public static ResultOrDiagnostic<IEnumerable<InjectorMemberTypeData>> GetInjectorMemberTypeDataOrError(
			IEnumerable<(MemberDeclarationSyntax memberSyntax, AttributeSyntax attribute)> membersInfo,
			SemanticModel semanticModel,
			INamedTypeSymbol classSymbol,
			ClassDeclarationSyntax classDeclaration,
			AttributeSyntax classInjectorAttribute
		) {
			Dictionary<ITypeSymbol, InjectorMemberData> typeInjectors = new Dictionary<ITypeSymbol, InjectorMemberData>(SymbolEqualityComparer.Default);

			foreach ((MemberDeclarationSyntax memberSyntax, AttributeSyntax attribute) in membersInfo) {
				ResultOrDiagnostic<TypeSyntax> memberTypeSyntax = GetMemberTypeSyntax(memberSyntax);
				if (memberTypeSyntax.HasDiagnostic) return memberTypeSyntax.Diagnostic;
				if (!memberTypeSyntax.HasResult) continue;

				if (!(semanticModel.GetTypeInfo(memberTypeSyntax.Result).Type is ITypeSymbol typeSymbol)) continue;

				ITypeSymbol nonNullableTypeSymbol = typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
				if (typeInjectors.TryGetValue(nonNullableTypeSymbol, out _)) {
					return Diagnostic.Create(DiagnosticDescriptors.MultipleInjectorsOfTypeDescriptor, classDeclaration.Identifier.GetLocation(), classDeclaration.Identifier.Text);
				}
				typeInjectors[nonNullableTypeSymbol] = (memberSyntax, attribute);
			}

			if (!(classInjectorAttribute is null)) {
				ITypeSymbol nonNullableClassSymbol = classSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
				if (typeInjectors.TryGetValue(nonNullableClassSymbol, out _)) {
					return Diagnostic.Create(DiagnosticDescriptors.MultipleInjectorsOfTypeDescriptor, classDeclaration.Identifier.GetLocation(), classDeclaration.Identifier.Text);
				}
				typeInjectors[nonNullableClassSymbol] = (classDeclaration, classInjectorAttribute);
			}

			return ResultOrDiagnostic<IEnumerable<InjectorMemberTypeData>>.FromResult(
				typeInjectors.Select(pair => new InjectorMemberTypeData(pair.Key, pair.Value))
			);
		}


		private static ResultOrDiagnostic<TypeSyntax> GetMemberTypeSyntax(MemberDeclarationSyntax memberSyntax) {
			switch (memberSyntax) {
				case MethodDeclarationSyntax methodDeclaration:
					if (methodDeclaration.ParameterList.Parameters.Count > 0) {
						return Diagnostic.Create(DiagnosticDescriptors.BadInjectorMethodParametersDescriptor, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.Text);
					}
					if (methodDeclaration.ReturnType is PredefinedTypeSyntax predefinedReturnType && predefinedReturnType.Keyword.IsKind(SyntaxKind.VoidKeyword)) {
						return Diagnostic.Create(DiagnosticDescriptors.VoidReturnTypeMethodDescriptor, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.Text);
					}
					return methodDeclaration.ReturnType;
				case PropertyDeclarationSyntax propertyDeclaration:
					bool hasGetter =
						propertyDeclaration.AccessorList?.Accessors.Any(SyntaxKind.GetAccessorDeclaration) ?? false ||
						propertyDeclaration.ExpressionBody is ArrowExpressionClauseSyntax;

					if (!hasGetter) {
						return Diagnostic.Create(DiagnosticDescriptors.GetterlessPropertyDescriptor, propertyDeclaration.Identifier.GetLocation(), propertyDeclaration.Identifier.Text);
					}
					return propertyDeclaration.Type;
				case FieldDeclarationSyntax fieldDeclaration:
					return fieldDeclaration.Declaration.Type;
				default:
					return default;
			}
		}
	}
}