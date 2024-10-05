namespace SevenDev.Boundless.Injection.Generators;

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class GeneratorExtensions {
	public static AttributeSyntax SelectOfType(this SyntaxList<AttributeListSyntax> attributesLists, Type type, SemanticModel semanticModel, CancellationToken cancellationToken = default) =>
		attributesLists
			.SelectMany(attrList => attrList.Attributes)
			.FirstOrDefault(attribute =>
				semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol?.ContainingSymbol is INamedTypeSymbol attributeSymbol
				&& attributeSymbol.ToDisplayString() == type.FullName
			);

	public static AttributeSyntax SelectOfType<T>(this SyntaxList<AttributeListSyntax> attributesLists, SemanticModel semanticModel, CancellationToken cancellationToken = default) =>
		attributesLists.SelectOfType(typeof(T), semanticModel, cancellationToken);
}