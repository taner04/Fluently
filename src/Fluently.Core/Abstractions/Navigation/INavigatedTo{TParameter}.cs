namespace Fluently;

/// <summary>
///     A page that becomes the current page with a parameter of type <typeparamref name="TParameter" />.
///     Navigate to it with
///     <see
///         cref="NavigationServiceExtensions.NavigateToAsync{TPage, TParameter}(INavigationService, TParameter, CancellationToken)" />
///     .
///     A page can implement this interface for several parameter types, and together with
///     <see cref="INavigatedTo" /> when it can also be shown without a parameter.
/// </summary>
/// <typeparam name="TParameter">The type of the parameter the page is navigated to with.</typeparam>
public interface INavigatedTo<in TParameter> : INavigableViewModel
    where TParameter : notnull
{
    /// <summary>
    ///     Called when the page becomes the current page.
    ///     Throwing cancels the navigation and restores the previous page.
    /// </summary>
    /// <param name="parameter">The parameter passed to the navigation.</param>
    /// <param name="cancellationToken">Canceled when the navigation is canceled.</param>
    /// <returns>A task that completes when the page is ready.</returns>
    Task OnNavigatedToAsync(TParameter parameter, CancellationToken cancellationToken);
}
