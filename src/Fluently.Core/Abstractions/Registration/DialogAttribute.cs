namespace Fluently;

/// <summary>
///     Registers this dialog as transient by the generated <c>AddViewModels()</c>: a new instance every time it is shown.
///     The class must implement <see cref="IDialog{TResult}" />.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DialogAttribute : Attribute;
