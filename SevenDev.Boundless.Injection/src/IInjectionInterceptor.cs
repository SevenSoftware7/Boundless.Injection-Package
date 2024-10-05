namespace SevenDev.Boundless.Injection;

using Godot;

/// <summary>
/// Allows a Node to intercept an IInjector injection propagation and modify the propagated value for each child individually.
/// </summary>
/// <typeparam name="T">The type of value which can be intercepted</typeparam>
public interface IInjectionInterceptor<T> where T : notnull {
	/// <summary>
	/// Called when an injection propagation passes through this node.<para/>
	/// Is called for each child node to whom the injected value will be propagated and propagates the return value instead.
	/// </summary>
	/// <param name="child">The child which will receive and propagate the returned value</param>
	/// <param name="value">The value which was being propagated before the interception</param>
	/// <returns>The value which will be received by the given <paramref name="child"/></returns>
	public T? Intercept(Node child, T? value);
}