namespace SevenDev.Boundless.Injection;

using System.Collections.Generic;

using Logger = System.Action<string>;

/// <summary>
/// Utility methods for Injection Propagation
/// </summary>
public static class InjectionExtensions {
	/// <summary>
	/// Propagates the value of an <paramref name="injector"/> to all child Nodes of the same parent Node.
	/// </summary>
	/// <typeparam name="T">The type of value which will be propagated</typeparam>
	/// <param name="injector">The Node which will propagate the value to its children</param>
	/// <param name="logger">A logger which will be called with a message whenever a value is propagated</param>
	public static void PropagateInjection<T>(this IInjector<T> injector, Logger? logger = null) where T : notnull {
		IInjectionNode node = injector.InjectionNode;
		if (!node.IsReady) return;

		T? value = injector.GetInjectValue();

		logger?.Invoke($"Injection || Propagating {value} (type {typeof(T).Name}) to {node.NodeName} children");

		node.PropagateInjection(injector.GetInjectValue());
	}

	/// <summary>
	/// Propagates a given <paramref name="value"/> to all child Nodes of a <paramref name="parent"/> Node.
	/// </summary>
	/// <typeparam name="T">The type of value which will be propagated</typeparam>
	/// <param name="parent">The parent Node whose children will receive the value through propagation</param>
	/// <param name="value">The value which will be propagated to the child Nodes</param>
	public static void PropagateInjection<T>(this IInjectionNode parent, T? value) where T : notnull {
		if (!parent.IsReady) return;

		PropagateInjection(parent, value, true);
	}

	/// <summary>
	/// Propagates a given <paramref name="value"/> to all child Nodes of a <paramref name="parent"/> Node.
	/// </summary>
	/// <typeparam name="T">The type of value which will be propagated</typeparam>
	/// <param name="parent">The parent Node whose children will receive the value through propagation</param>
	/// <param name="value">The value which will be propagated to the child Nodes</param>
	/// <param name="skipParent">
	/// Whether the parent Node should be skipped when propagating the <paramref name="value"/> to its children or when blocking the propagation.<para/>
	/// <remark>
	/// This is mostly used to prevent injections from stopping immediately or looping infinitely.
	/// </remark>
	/// </param>
	private static void PropagateInjection<T>(in IInjectionNode parent, T? value, bool skipParent) where T : notnull {
		object? parentObject = parent.UnderlyingObject;
		IInjectionInterceptor<T>? interceptorParent = parentObject as IInjectionInterceptor<T>;
		IInjectionBlocker<T>? blockerParent = parentObject as IInjectionBlocker<T>;

		IEnumerable<IInjectionNode> children = parent.Children;

		List<(IInjectionNode child, T? childValue)> injections = new(children.Count());
		foreach (IInjectionNode child in children) {
			if (!skipParent && blockerParent is not null && blockerParent.ShouldBlock(child, value)) continue;

			T? childValue = interceptorParent is not null ? interceptorParent.Intercept(child, value) : value;
			injections.Add((child, childValue));
		}

		if (!skipParent && parentObject is IInjectable<T> injectableParent) {
			injectableParent.Inject(value);
		}

		foreach ((IInjectionNode child, T? childValue) in injections) {
			PropagateInjection(child, childValue, false);
		}
	}

	/// <summary>
	/// Used to request an Injection propagation of a value which the <paramref name="requester"/> Node depends on
	/// </summary>
	/// <typeparam name="T">The type of value which needs to be injected</typeparam>
	/// <param name="requester">The Node which requested an Injection propagation</param>
	/// <param name="logger">A logger which will be called with a message whenever a value is propagated</param>
	/// <returns>Whether a fitting <see cref="IInjector{T}"/> was found and a value was injected to the original <paramref name="requester"/> Node</returns>
	/// <remark>
	/// In the case that the <paramref name="requester"/> Node is not ready (see <see cref="IInjectionNode.IsReady"/>), the injection will not request the propagation and will return true.
	/// </remark>
	public static bool RequestInjection<T>(this IInjectable<T> requester, Logger? logger = null) where T : notnull {
		IInjectionNode node = requester.InjectionNode;
		IInjectionNode? parent = node.Parent;
		if (parent is null) return false;
		if (!parent.IsReady) return true; // Don't request Injection if the parents are not ready, they will inject when they are (if they can)

		logger?.Invoke($"Injection || Requesting Injection of {typeof(T).Name} for {node.NodeName}");

		return RequestInjection<T>(parent, logger);
	}

	/// <summary>
	/// Used to request an Injection propagation of a type of value which a <paramref name="requester"/> Node depends on
	/// </summary>
	/// <typeparam name="T">The type of value which needs to be injected</typeparam>
	/// <param name="requester">The Node (or one of its ancesters) which requested an Injection propagation</param>
	/// <param name="logger">A logger which will be called with a message whenever a value is propagated</param>
	/// <returns>Whether a fitting <see cref="IInjector{T}"/> was found and a value was injected to the original <paramref name="requester"/> Node</returns>
	private static bool RequestInjection<T>(in IInjectionNode requester, Logger? logger = null) where T : notnull {
		if (requester.UnderlyingObject is not IInjector<T> provider) {
			logger?.Invoke($"Injection || Requesting {typeof(T).Name} Injection at {requester.NodeName}");

			if (requester.Parent is null) return false;
			return RequestInjection<T>(requester.Parent, logger);
		}

		logger?.Invoke($"Injection || Found {typeof(T).Name} Injector: {requester.NodeName}");

		PropagateInjection(requester, provider.GetInjectValue(), true);
		return true;
	}
}