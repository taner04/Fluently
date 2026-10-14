namespace Fluently;

/// <summary>
/// A page that reacts to no longer being the current page.
/// </summary>
public interface INavigatedFrom : INavigableViewModel
{
    /// <summary>
    /// Called when the page stops being the current page.
    /// </summary>
    Task OnNavigatedFromAsync(CancellationToken cancellationToken);
}
