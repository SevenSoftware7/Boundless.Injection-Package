using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Seven.Boundless.Injection.Generators {

	public static class GeneratorExtensions {
		public static AttributeSyntax SelectOfType(this SyntaxList<AttributeListSyntax> attributesLists, Type type, SemanticModel semanticModel, CancellationToken cancellationToken = default) =>
			attributesLists
				.SelectMany(attrList => attrList.Attributes)
				.FirstOrDefault(attribute =>
					semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol?.ContainingSymbol is INamedTypeSymbol attributeSymbol
					&& attributeSymbol.ToDisplayString() == type.FullName
				);

		public static AttributeSyntax SelectOfTypeName(this SyntaxList<AttributeListSyntax> attributesLists, string typeName, SemanticModel semanticModel, CancellationToken cancellationToken = default) =>
			attributesLists
				.SelectMany(attrList => attrList.Attributes)
				.FirstOrDefault(attribute =>
					semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol?.ContainingSymbol is INamedTypeSymbol attributeSymbol
					&& attributeSymbol.ToDisplayString() == typeName
				);

		public static AttributeSyntax SelectOfType<T>(this SyntaxList<AttributeListSyntax> attributesLists, SemanticModel semanticModel, CancellationToken cancellationToken = default) =>
			attributesLists.SelectOfType(typeof(T), semanticModel, cancellationToken);
	}
}