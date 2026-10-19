namespace Fluently;

/// <summary>
///     A view model that can be shown as a toast by an <see cref="IToastService" />.
///     Use the built-in <see cref="Toast" /> for a title and a message, or implement this interface for a toast with
///     its own view, for example one with an Undo button.
/// </summary>
public interface IToast;
