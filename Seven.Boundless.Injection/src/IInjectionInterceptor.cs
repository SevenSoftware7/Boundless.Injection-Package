namespace Seven.Boundless.Injection;

/// <summary>
/// Allows a Node to intercept an IInjector injection propagation and modify the propagated value for each child individually.
/// </summary>
/// <typeparam name="T">The type of value which can be intercepted</typeparam>
public interface IInjectionInterceptor<T> where T : notnull {
	/// <summary>
	/// Called when an injection propagation passes through this node.<para/>
	/// Invoked for each <paramref name="child"/> Node, allowing the interception and modification of the propagated <paramref name="value"/> before it reaches the child.
	/// </summary>
	/// <param name="child">The child which will receive and propagate the returned value</param>
	/// <param name="value">The value which was being propagated before the interception</param>
	/// <returns>The value which will be received by the given <paramref name="child"/></returns>
	public T? Intercept(IInjectionNode child, T? value);
}