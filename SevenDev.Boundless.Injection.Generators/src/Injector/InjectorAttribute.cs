using System;

namespace SevenDev.Boundless.Injection {

	/// <summary>
	/// Attribute used to indicate that a Node's class member (method, property or field), or the Node itself, is a value that can be injected to dependant Nodes.<para/>
	/// Using this attribute will generate the corresponding IInjector Method implementations.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
	public class InjectorAttribute : Attribute {
		internal static readonly Type CachedType = typeof(InjectorAttribute);

		public InjectorAttribute() { }
	}
}