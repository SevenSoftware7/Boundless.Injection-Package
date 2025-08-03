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
	public readonly Node GodotNode;


	public GodotNodeInjectionNode(Node godotNode) {
		ArgumentNullException.ThrowIfNull(godotNode);
		GodotNode = godotNode;
	}


	/// <summary>
	/// Returns the underlying Godot.Node of the current node.
	/// </summary>
	public readonly Node UnderlyingObject => GodotNode;
	readonly object IInjectionNode.UnderlyingObject => UnderlyingObject;

	/// <inheritdoc/>
	public readonly IInjectionNode? Parent => GodotNode.GetParent() is Node parent ? new GodotNodeInjectionNode(parent) : null;

	/// <inheritdoc/>
	public readonly IEnumerable<IInjectionNode> Children {
		get {
			foreach (Node child in GodotNode.GetChildren()) {
				yield return new GodotNodeInjectionNode(child);
			}
		}
	}

	/// <inheritdoc/>
	public readonly bool IsTreeReady =>
		(GodotNode.GetParent()?.IsNodeReady() ?? false) ||
		(GodotNode.GetTree()?.Root?.IsNodeReady() ?? false);

	/// <inheritdoc/>
	public readonly string NodeName => GodotNode.Name;
}