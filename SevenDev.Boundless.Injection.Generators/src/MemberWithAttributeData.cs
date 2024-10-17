using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SevenDev.Boundless.Injection.Generators {

	internal readonly struct MemberWithAttributeData {
		public MemberDeclarationSyntax MemberDeclaration { get; }
		public AttributeSyntax Attribute { get; }

		public MemberWithAttributeData(MemberDeclarationSyntax MemberDeclaration, AttributeSyntax Attribute) {
			this.MemberDeclaration = MemberDeclaration;
			this.Attribute = Attribute;
		}


		public static implicit operator (MemberDeclarationSyntax memberDeclaration, AttributeSyntax attribute)(MemberWithAttributeData value) {
			return (value.MemberDeclaration, value.Attribute);
		}

		public static implicit operator MemberWithAttributeData((MemberDeclarationSyntax memberDeclaration, AttributeSyntax attribute) value) {
			return new MemberWithAttributeData(value.memberDeclaration, value.attribute);
		}
	}
}