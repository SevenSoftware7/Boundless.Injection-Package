namespace SevenDev.Boundless.Injection;

using System;

/// <summary>
/// Attribute used to indicate that a Node class member (method, property or field) is an Injectable member and can be used to inject a value into the Node script.<para/>
/// Using this attribute will generate the corresponding IInjectable Method implementations.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class InjectableAttribute : Attribute {
	internal static readonly Type CachedType = typeof(InjectableAttribute);

	/// <summary>
	/// The priority of the Injectable member in the Injection method.<para/>
	/// Higher priority values mean the member will be updated first.
	/// <example>
	/// <code>
	/// [Injectable(1)]<br/>
	/// public float Value1 { get; set; }
	///
	/// [Injectable(2)]<br/>
	/// public float Value2 { get; set; }
	/// </code>
	/// </example>
	/// Value2 will be updated before Value1
	/// </summary>
	public int Priority;

	/// <param name="priority">
	/// The priority of the targeted member, it dictates how early a specific member will be updated when a value is injected.<para/>
	/// See <see cref="Priority"/>.
	/// </param>
	public InjectableAttribute(int priority = 0) {
		Priority = priority;
	}
}