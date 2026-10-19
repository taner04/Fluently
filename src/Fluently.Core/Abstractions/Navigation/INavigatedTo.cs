namespace Fluently;

/// <summary>
///     A page that reacts to becoming the current page without a navigation parameter.
///     Implement <see cref="INavigatedTo{TParameter}" /> instead when the page needs a parameter.
/// </summary>
public interface INavigatedTo : INavigableViewModel
{
    /// <summary>
    ///     Called when the page becomes the current page.
    ///     Throwing cancels the navigation and restores the previous page.
    /// </summary>
    /// <param name="cancellationToken">Canceled when the navigation is canceled.</param>
    /// <returns>A task that completes when the page is ready.</returns>
    Task OnNavigatedToAsync(CancellationToken cancellationToken);
}
