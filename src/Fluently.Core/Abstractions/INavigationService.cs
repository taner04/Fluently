namespace Fluently;

/// <summary>
/// Navigates between <see cref="INavigableViewModel"/> pages and keeps a back and forward history.
/// Every history entry resolves its page from its own service scope, so a transient page is created
/// on every navigation, and the scope is disposed together with its disposable services when the entry
/// leaves the history. Disposing the service releases all entries.
/// </summary>
public interface INavigationService : IDisposable
{
    /// <summary>
    /// The page that is currently shown, or <see langword="null"/> before the first navigation.
    /// </summary>
    INavigableViewModel? CurrentPage { get; }

    /// <summary>
    /// Whether there is a previous page to go back to.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Whether there is a next page to go forward to.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// Raised after a navigation completed. <see cref="CanGoBack"/> and <see cref="CanGoForward"/>
    /// only change with a navigation, so this is the moment to re-evaluate commands that use them.
    /// </summary>
    event EventHandler<NavigatedEventArgs>? Navigated;

    /// <summary>
    /// Navigates to the page of the given view model type.
    /// </summary>
    /// <returns><see langword="false"/> when the page is already shown or entering it failed.</returns>
    Task<bool> NavigateToAsync(Type viewModelType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Navigates to the page of the given view model type and passes it a parameter.
    /// Navigating to the current page with a different parameter replaces the current history entry.
    /// </summary>
    /// <returns><see langword="false"/> when the page is already shown with this parameter or entering it failed.</returns>
    Task<bool> NavigateToAsync(
        Type viewModelType,
        object parameter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Navigates to the previous page.
    /// </summary>
    /// <returns><see langword="false"/> when there is no previous page or entering it failed.</returns>
    Task<bool> GoBackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Navigates to the next page after going back.
    /// </summary>
    /// <returns><see langword="false"/> when there is no next page or entering it failed.</returns>
    Task<bool> GoForwardAsync(CancellationToken cancellationToken = default);
}
