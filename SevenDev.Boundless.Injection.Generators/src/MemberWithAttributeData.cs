using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SevenDev.Boundless.Injection.Generators {

	internal readonly struct MemberAndAttributeData {
		public MemberDeclarationSyntax MemberDeclaration { get; }
		public AttributeSyntax Attribute { get; }

		public MemberAndAttributeData(MemberDeclarationSyntax MemberDeclaration, AttributeSyntax Attribute) {
			this.MemberDeclaration = MemberDeclaration;
			this.Attribute = Attribute;
		}


		public static implicit operator (MemberDeclarationSyntax memberDeclaration, AttributeSyntax attribute)(MemberAndAttributeData value) {
			return (value.MemberDeclaration, value.Attribute);
		}

		public static implicit operator MemberAndAttributeData((MemberDeclarationSyntax memberDeclaration, AttributeSyntax attribute) value) {
			return new MemberAndAttributeData(value.memberDeclaration, value.attribute);
		}
	}
}