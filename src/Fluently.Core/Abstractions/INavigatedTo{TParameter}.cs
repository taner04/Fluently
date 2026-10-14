namespace Fluently;

/// <summary>
/// A page that reacts to becoming the current page and expects a parameter of type <typeparamref name="TParameter"/>.
/// Navigate to it with <c>NavigateToAsync&lt;TPage, TParameter&gt;</c> to have the parameter type checked at compile time.
/// </summary>
public interface INavigatedTo<in TParameter> : INavigatedTo
    where TParameter : notnull
{
    /// <summary>
    /// Called when the page becomes the current page.
    /// Throwing cancels the navigation and restores the previous page.
    /// </summary>
    Task OnNavigatedToAsync(TParameter parameter, CancellationToken cancellationToken);

    Task INavigatedTo.OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken) =>
        parameter is TParameter typedParameter
            ? OnNavigatedToAsync(typedParameter, cancellationToken)
            : Task.FromException(new ArgumentException(
                $"{GetType().Name} expects a navigation parameter of type {typeof(TParameter).Name}, but got {parameter?.GetType().Name ?? "none"}.",
                nameof(parameter)));
}
