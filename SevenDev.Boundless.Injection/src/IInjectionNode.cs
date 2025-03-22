namespace SevenDev.Boundless.Injection;

using System.Collections.Generic;

/// <summary>
/// Represents a node in an injection hierarchy.
/// </summary>
public interface IInjectionNode {
	/// <summary>
	/// Returns the underlying object of the current node.
	/// </summary>
	public object UnderlyingObject { get; }

	/// <summary>
	/// Gets the parent node of the current node.
	/// </summary>
	/// <returns>The parent <see cref="IInjectionNode"/> if it exists; otherwise, <c>null</c>.</returns>
	public IInjectionNode? Parent { get; }

	/// <summary>
	/// Gets the child nodes of the current node.
	/// </summary>
	/// <returns>An <see cref="IEnumerable{T}"/> of child <see cref="IInjectionNode"/> objects.</returns>
	public IEnumerable<IInjectionNode> Children { get; }

	/// <summary>
	/// Determines whether the node's injection tree is ready to inject.
	/// </summary>
	/// <returns><c>true</c> if the injection tree is ready; otherwise, <c>false</c>.</returns>
	public bool IsTreeReady { get; }

	/// <summary>
	/// Gets the name of the current node.
	/// </summary>
	/// <returns>A <see cref="string"/> representing the name of the node.</returns>
	public string? NodeName { get; }
}