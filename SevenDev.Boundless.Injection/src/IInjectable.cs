namespace SevenDev.Boundless.Injection;

/// <summary>
/// Simple Injection interface which gives access to a method to call for all injection dependencies on a script.
/// </summary>
public interface IInjectable {
	/// <summary>
	/// Request the injection of all IInjectable dependencies.<para/>
	/// This method does nothing by default and must be implemented manually.
	/// </summary>
	/// <example>
	/// <code>
	/// public virtual void RequestInjection()
	/// {
	/// 	this.RequestInjection&lt;Skeleton3D&gt;();
	/// }
	/// </code>
	/// </example>
	public void RequestInjection() { }
}

/// <summary>
/// Allows a Node to receive dependency-injected values via 'injection propagation'
/// </summary>
/// <typeparam name="T">The type of values which will be injected to the node</typeparam>
public interface IInjectable<in T> : IInjectable, IInjectionNodeProvider where T : notnull {
	/// <summary>
	/// Method that is called whenever a <paramref name="value"/> of type T is injected to this node.
	/// </summary>
	/// <param name="value">The value which was injected to this Node</param>
	public void Inject(T? value);
}