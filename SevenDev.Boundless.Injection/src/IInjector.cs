namespace SevenDev.Boundless.Injection;

/// <summary>
/// Allows a Node to provide a value of type T, be it by responding to injection requests or by manually propagating an injection.
/// </summary>
/// <typeparam name="T">The type of value which will be injected</typeparam>
public interface IInjector<out T> : IInjectionNodeProvider where T : notnull {
	/// <summary>
	/// Gets the value which will be injected.
	/// </summary>
	/// <returns>The injection value</returns>
	public T? GetInjectValue();
}