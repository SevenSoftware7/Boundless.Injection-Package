namespace SevenDev.Boundless.Injection;

using Godot;

/// <summary>
/// Allows a Node to block injection propagation to select children and/or for select injection values.
/// </summary>
/// <typeparam name="T">the type of value which will be filtered/blocked</typeparam>
public interface IInjectionBlocker<in T> where T : notnull {
	/// <summary>
	/// Returns whether a certain injection <paramref name="value"/> should be blocked from propagating to a given <paramref name="child"/> Node.
	/// </summary>
	/// <param name="child">The child Node which could be blocked from being propagated to</param>
	/// <param name="value">The value which will be propagated to the child in the case that propagation is not blocked</param>
	/// <returns>Whether the injection propagation will be blocked</returns>
	bool ShouldBlock(Node child, T? value) => true;
}