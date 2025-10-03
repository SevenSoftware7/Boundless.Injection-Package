namespace Seven.Boundless.Injection.Godot;

using System;
using System.Collections.Generic;
using global::Godot;
using Seven.Boundless.Injection;

/// <summary>
/// Represents a Godot node in an injection hierarchy.
/// </summary>
/// <seealso cref="IInjectionNode"/>
/// <seealso cref="Node"/>
public readonly struct GodotNodeInjectionNode : IInjectionNode {
	/// <summary>
	/// The underlying Godot node.
	/// </summary>
	public readonly Node UnderlyingNode;
	readonly object IInjectionNode.UnderlyingObject => UnderlyingNode;

	/// <inheritdoc/>
	public readonly string NodeName => UnderlyingNode.Name;

	/// <inheritdoc/>
	public readonly IInjectionNode? Parent => UnderlyingNode.GetParent() is Node parent ? new GodotNodeInjectionNode(parent) : null;

	/// <inheritdoc/>
	public readonly bool IsTreeReady =>
		(UnderlyingNode.GetParent()?.IsNodeReady() ?? false) ||
		(UnderlyingNode.GetTree()?.Root?.IsNodeReady() ?? false);

	/// <inheritdoc/>
	public readonly IEnumerable<IInjectionNode> Children {
		get {
			foreach (Node child in UnderlyingNode.GetChildren()) {
				yield return new GodotNodeInjectionNode(child);
			}
		}
	}


	/// <summary>
	/// Initializes a new instance of the <see cref="GodotNodeInjectionNode"/>.
	/// </summary>
	/// <param name="godotNode">The underlying Godot node.</param>
	public GodotNodeInjectionNode(Node godotNode) {
		ArgumentNullException.ThrowIfNull(godotNode);
		UnderlyingNode = godotNode;
	}
}