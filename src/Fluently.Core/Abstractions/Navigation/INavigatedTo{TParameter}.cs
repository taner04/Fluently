namespace Fluently;

/// <summary>
/// A page that becomes the current page with a parameter of type <typeparamref name="TParameter"/>.
/// Navigate to it with <see cref="INavigationService.NavigateToAsync{TPage, TParameter}"/>.
/// A page can implement this interface for several parameter types, and together with
/// <see cref="INavigatedTo"/> when it can also be shown without a parameter.
/// </summary>
public interface INavigatedTo<in TParameter> : INavigableViewModel
    where TParameter : notnull
{
    /// <summary>
    /// Called when the page becomes the current page.
    /// Throwing cancels the navigation and restores the previous page.
    /// </summary>
    Task OnNavigatedToAsync(TParameter parameter, CancellationToken cancellationToken);
}
