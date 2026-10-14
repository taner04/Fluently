namespace Fluently;

/// <summary>
/// A page that reacts to becoming the current page.
/// </summary>
public interface INavigatedTo : INavigableViewModel
{
    /// <summary>
    /// Called when the page becomes the current page.
    /// Throwing cancels the navigation and restores the previous page.
    /// </summary>
    Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken);
}
