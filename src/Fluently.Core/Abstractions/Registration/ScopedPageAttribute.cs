namespace Fluently;

/// <summary>
///     Registers this page as scoped by the generated <c>AddViewModels()</c>: one instance per history entry, which the
///     other services of that entry's scope can inject.
///     The class must implement <see cref="INavigableViewModel" />.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ScopedPageAttribute : Attribute;
