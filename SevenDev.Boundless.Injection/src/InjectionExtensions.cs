namespace SevenDev.Boundless.Injection;

using Godot;

/// <summary>
/// Utility methods for Injection Propagation
/// </summary>
public static class InjectionExtensions {
	/// <summary>
	/// Propagates the value of an <paramref name="injector"/> to all child Nodes of the same parent Node.
	/// </summary>
	/// <typeparam name="T">The type of value which will be propagated</typeparam>
	/// <param name="injector">The Node which will propagate the value to its children</param>
	public static void PropagateInjection<T>(this IInjector<T> injector) where T : notnull {
		if (injector is not Node injectorParent) return;

		injectorParent.PropagateInjection(injector.GetInjectValue());
	}

	/// <summary>
	/// Propagates a given <paramref name="value"/> to all child Nodes of a <paramref name="parent"/> Node.
	/// </summary>
	/// <typeparam name="T">The type of value which will be propagated</typeparam>
	/// <param name="parent">The parent Node whose children will receive the value through propagation</param>
	/// <param name="value">The value which will be propagated to the child Nodes</param>
	public static void PropagateInjection<T>(this Node parent, T? value) where T : notnull {
		if (!parent.IsNodeReady()) return;
		GD.Print($"Propagating {value} (type {typeof(T).Name}) to {parent.Name} children");

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
	private static void PropagateInjection<T>(Node parent, T? value, bool skipParent) where T : notnull {
		if (!skipParent && parent is IInjectable<T> injectableParent) {
			injectableParent.Inject(value);
		}

		IInjectionInterceptor<T>? interceptorParent = parent as IInjectionInterceptor<T>;
		IInjectionBlocker<T>? blockerParent = parent as IInjectionBlocker<T>;

		foreach (Node child in parent.GetChildren()) {
			if (!skipParent && blockerParent is not null && blockerParent.ShouldBlock(child, value)) continue;

			T? childValue = interceptorParent is not null ? interceptorParent.Intercept(child, value) : value;
			PropagateInjection(child, childValue, false);
		}
	}

	/// <summary>
	/// Used to request an Injection propagation of a value which the <paramref name="requester"/> Node depends on
	/// </summary>
	/// <typeparam name="T">The type of value which needs to be injected</typeparam>
	/// <param name="requester">The Node which requested an Injection propagation</param>
	/// <returns>Whether a fitting <see cref="IInjector{T}"/> was found and a value was injected to the original <paramref name="requester"/> Node</returns>
	/// <remark>
	/// In the case that the <paramref name="requester"/> Node is not ready (see <see cref="Node.IsNodeReady"/>), the injection will not request the propagation and will return true.
	/// </remark>
	public static bool RequestInjection<T>(this IInjectable<T> requester) where T : notnull {
		if (requester is not Node requesterNode || requesterNode.GetParent() is not Node requesterParent) return false;
		if (!requesterParent.IsNodeReady()) return true; // Don't request Injection if the parents are not ready, they will inject when they are (if they can)

		GD.Print($"Requesting Injection of {typeof(T).Name} for {requesterNode.Name}");

		return requesterParent.RequestInjection<T>();
	}

	/// <summary>
	/// Used to request an Injection propagation of a type of value which a <paramref name="requester"/> Node depends on
	/// </summary>
	/// <typeparam name="T">The type of value which needs to be injected</typeparam>
	/// <param name="requester">The Node (or one of its ancesters) which requested an Injection propagation</param>
	/// <returns>Whether a fitting <see cref="IInjector{T}"/> was found and a value was injected to the original <paramref name="requester"/> Node</returns>
	private static bool RequestInjection<T>(this Node requester) where T : notnull {
		if (requester is not IInjector<T> provider) {
			return requester.GetParent()?.RequestInjection<T>() ?? false;
		}

		PropagateInjection(requester, provider.GetInjectValue(), true);
		return true;
	}
}