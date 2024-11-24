

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SevenDev.Boundless.Injection.Generators {
	public static class Utility {

		public static readonly SymbolDisplayFormat ClassDeclarationNameFormat = new SymbolDisplayFormat(
			SymbolDisplayGlobalNamespaceStyle.Omitted,
			SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
			SymbolDisplayGenericsOptions.IncludeTypeParameters,
			kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword,
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
		);

		public static string GetFileLegalTypeString(ClassDeclarationSyntax declaration, SemanticModel semanticModel) {
			ISymbol symbol = semanticModel.GetDeclaredSymbol(declaration);
			return symbol.ToMinimalDisplayString(semanticModel, declaration.SpanStart, SymbolDisplayFormat.MinimallyQualifiedFormat)
				.Replace("<", "(Of ")
				.Replace(">", ")");
		}

		public static string GetSymbolAccessibility(ISymbol symbol) {
			switch (symbol.DeclaredAccessibility) {
				case Accessibility.Public:
					return "public";
				case Accessibility.Protected:
					return "protected";
				case Accessibility.Internal:
					return "internal";
				case Accessibility.Private:
					return "private";
				case Accessibility.ProtectedAndInternal:
					return "protected internal";
				case Accessibility.ProtectedOrInternal:
					return "private protected";
				default:
					return "";
			}
		}
	}
}

