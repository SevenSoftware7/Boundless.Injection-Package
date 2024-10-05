namespace SevenDev.Boundless.Injection.Generators;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal record struct MemberWithAttributeData(MemberDeclarationSyntax MemberDeclaration, AttributeSyntax Attribute) {
	public static implicit operator (MemberDeclarationSyntax memberDeclaration, AttributeSyntax attribute)(MemberWithAttributeData value) {
		return (value.MemberDeclaration, value.Attribute);
	}

	public static implicit operator MemberWithAttributeData((MemberDeclarationSyntax memberDeclaration, AttributeSyntax attribute) value) {
		return new MemberWithAttributeData(value.memberDeclaration, value.attribute);
	}
}