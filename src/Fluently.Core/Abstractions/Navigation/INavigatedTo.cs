namespace Fluently;

/// <summary>
/// A page that reacts to becoming the current page without a navigation parameter.
/// Implement <see cref="INavigatedTo{TParameter}"/> instead when the page needs a parameter.
/// </summary>
public interface INavigatedTo : INavigableViewModel
{
    /// <summary>
    /// Called when the page becomes the current page.
    /// Throwing cancels the navigation and restores the previous page.
    /// </summary>
    Task OnNavigatedToAsync(CancellationToken cancellationToken);
}
