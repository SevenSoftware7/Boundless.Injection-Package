using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Seven.Boundless.Injection.Generators {

	internal readonly struct InjectableMemberData {
		public MemberDeclarationSyntax MemberDeclaration { get; }
		public AttributeSyntax Attribute { get; }
		public int Priority { get; }

		public InjectableMemberData(MemberDeclarationSyntax MemberDeclaration, AttributeSyntax Attribute, int Priority = 0) {
			this.MemberDeclaration = MemberDeclaration;
			this.Attribute = Attribute;
			this.Priority = Priority;
		}


		public static implicit operator (MemberDeclarationSyntax memberDeclaration, AttributeSyntax attribute, int priority)(InjectableMemberData value) {
			return (value.MemberDeclaration, value.Attribute, value.Priority);
		}

		public static implicit operator InjectableMemberData((MemberDeclarationSyntax memberDeclaration, AttributeSyntax attribute, int priority) value) {
			return new InjectableMemberData(value.memberDeclaration, value.attribute, value.priority);
		}
	}
}