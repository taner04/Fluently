namespace Fluently;

/// <summary>
///     Registers this page as a singleton by the generated <c>AddViewModels()</c>: one instance for the whole application,
///     so the page keeps its state when it is shown again.
///     The class must implement <see cref="INavigableViewModel" />.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SingletonPageAttribute : Attribute;
