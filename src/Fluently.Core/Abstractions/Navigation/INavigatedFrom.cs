namespace Fluently;

/// <summary>
///     A page that reacts to no longer being the current page.
/// </summary>
public interface INavigatedFrom : INavigableViewModel
{
    /// <summary>
    ///     Called when the page stops being the current page, before the next page is entered.
    /// </summary>
    /// <param name="cancellationToken">Canceled when the navigation is canceled.</param>
    /// <returns>A task that completes when the page is done leaving.</returns>
    Task OnNavigatedFromAsync(CancellationToken cancellationToken);
}
