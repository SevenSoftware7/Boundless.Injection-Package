using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Seven.Boundless.Injection.Generators {

	internal readonly struct InjectorMemberData {
		public MemberDeclarationSyntax MemberDeclaration { get; }
		public AttributeSyntax Attribute { get; }

		public InjectorMemberData(MemberDeclarationSyntax MemberDeclaration, AttributeSyntax Attribute) {
			this.MemberDeclaration = MemberDeclaration;
			this.Attribute = Attribute;
		}


		public static implicit operator (MemberDeclarationSyntax memberDeclaration, AttributeSyntax attribute)(InjectorMemberData value) {
			return (value.MemberDeclaration, value.Attribute);
		}

		public static implicit operator InjectorMemberData((MemberDeclarationSyntax memberDeclaration, AttributeSyntax attribute) value) {
			return new InjectorMemberData(value.memberDeclaration, value.attribute);
		}
	}
}