namespace SevenDev.Boundless.Injection;

/// <summary>
/// Provides a method to retrieve an injection node.
/// </summary>
public interface IInjectionNodeProvider {
	/// <summary>
	/// Retrieves an injection node.
	/// </summary>
	/// <returns>An instance of <see cref="IInjectionNode"/> if available; otherwise, <c>null</c>.</returns>
	IInjectionNode InjectionNode { get; }
}