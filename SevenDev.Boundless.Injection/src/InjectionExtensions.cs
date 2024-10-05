namespace SevenDev.Boundless.Injection;

using Godot;

/// <summary>
/// Utility methods for Injection Propagation
/// </summary>
public static class InjectionExtensions {
	/// <summary>
	/// Propagate an injection propagation using <paramref name="parent">this node</paramref> as a provider for the value
	/// </summary>
	/// <typeparam name="T">The type of value which will be propagated</typeparam>
	/// <param name="parent">The Node which will propagate the value</param>
	public static void PropagateInject<T>(this IInjector<T> parent) where T : notnull {
		if (parent is not Node nodeParent) return;

		nodeParent.PropagateInject(parent.GetInjectValue(), true);
	}

	/// <summary>
	/// Propagates a given <paramref name="value"/> to all child Nodes of a <paramref name="parent"/> Node.
	/// </summary>
	/// <typeparam name="T">The type of value which will be propagated</typeparam>
	/// <param name="parent">The parent Node whose children will receive the value through propagation</param>
	/// <param name="value">The value which will be propagated to the child Nodes</param>
	/// <param name="ignoreParentBlocker">
	/// Whether the propagation should not stop if the given <paramref name="parent"/> Node is an <see cref="IInjectionBlocker{T}"/><para/>
	/// <remark>
	/// This is mostly used to prevent injections from stopping immediately when calling <see cref="PropagateInject{T}(IInjector{T})"/>
	/// </remark>
	/// </param>
	public static void PropagateInject<T>(this Node parent, T? value, bool ignoreParentBlocker = false) where T : notnull {
		if (parent is IInjectable<T> injectableParent) {
			injectableParent.Inject(value);
		}

		IInjectionInterceptor<T>? interceptorParent = parent as IInjectionInterceptor<T>;
		IInjectionBlocker<T>? blockerParent = parent as IInjectionBlocker<T>;

		foreach (Node child in parent.GetChildren()) {
			if (!ignoreParentBlocker && blockerParent is not null && blockerParent.ShouldBlock(child, value)) continue;

			T? childValue = interceptorParent is not null ? interceptorParent.Intercept(child, value) : value;
			child.PropagateInject(childValue);
		}
	}

	/// <summary>
	/// Used to request an Injection propagation of a type of value which a <paramref name="requester"/> Node depends on
	/// </summary>
	/// <typeparam name="T">The type of value which needs to be injected</typeparam>
	/// <param name="requester">The Node (or one of its ancesters) which requested an Injection propagation</param>
	/// <returns>Whether a fitting <see cref="IInjector{T}"/> was found and a value was injected to the original <paramref name="requester"/> Node</returns>
	public static bool RequestInjection<T>(this Node requester) where T : notnull {
		if (requester is not IInjector<T> provider) {
			return requester.GetParent()?.RequestInjection<T>() ?? false;
		}

		provider.PropagateInject();
		return true;
	}
}