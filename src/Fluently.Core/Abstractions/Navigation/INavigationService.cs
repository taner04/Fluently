using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Fluently;

/// <summary>
///     Navigates between <see cref="INavigableViewModel" /> pages and keeps a back and forward history.
///     Every history entry resolves its page from its own service scope, so a transient page is created
///     on every navigation, and the scope is disposed together with its disposable services when the entry
///     leaves the history. Disposing the service releases all entries; prefer <see cref="IAsyncDisposable.DisposeAsync" />
///     ,
///     which also disposes services that can only be disposed asynchronously.
///     <see cref="CurrentPage" />, <see cref="CanGoBack" /> and <see cref="CanGoForward" /> raise
///     <see cref="INotifyPropertyChanged.PropertyChanged" />, so views can bind to them directly.
/// </summary>
public interface INavigationService : INotifyPropertyChanged, IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     The page that is currently shown, or <see langword="null" /> before the first navigation.
    /// </summary>
    INavigableViewModel? CurrentPage { get; }

    /// <summary>
    ///     Whether there is a previous page to go back to.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    ///     Whether there is a next page to go forward to.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    ///     Raised after a navigation completed, after the changed properties raised
    ///     <see cref="INotifyPropertyChanged.PropertyChanged" />.
    /// </summary>
    event EventHandler<NavigatedEventArgs>? Navigated;

    /// <summary>
    ///     Navigates to the page of view model type <paramref name="viewModelType" /> without a parameter.
    ///     <see cref="NavigationServiceExtensions.NavigateToAsync{TPage}(INavigationService, CancellationToken)" />
    ///     is the same with the type checked at compile time.
    /// </summary>
    /// <param name="viewModelType">The type of the page view model, registered in the container.</param>
    /// <param name="cancellationToken">
    ///     Cancels the navigation: it throws <see cref="OperationCanceledException" />, and the current page stays or, when it
    ///     was already being left, is entered again.
    /// </param>
    /// <returns>
    ///     <see langword="false" /> when the page is already shown, a guard keeps the current page,
    ///     the page needs a parameter, or entering it failed.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="viewModelType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="viewModelType" /> does not implement
    ///     <see cref="INavigableViewModel" />.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was canceled.</exception>
    /// <exception cref="ObjectDisposedException">The navigation service was disposed.</exception>
    Task<bool> NavigateToAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        Type viewModelType,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Navigates to the page of view model type <paramref name="viewModelType" /> and passes it
    ///     <paramref name="parameter" /> through <see cref="INavigatedTo{TParameter}" />.
    ///     Navigating to the current page with a different parameter replaces the current history entry.
    ///     <see
    ///         cref="NavigationServiceExtensions.NavigateToAsync{TPage, TParameter}(INavigationService, TParameter, CancellationToken)" />
    ///     is the same with the page type checked at compile time.
    /// </summary>
    /// <typeparam name="TParameter">
    ///     The type of the parameter; the page receives it through
    ///     <see cref="INavigatedTo{TParameter}" />.
    /// </typeparam>
    /// <param name="viewModelType">The type of the page view model, registered in the container.</param>
    /// <param name="parameter">The parameter passed to the page.</param>
    /// <param name="cancellationToken">
    ///     Cancels the navigation: it throws <see cref="OperationCanceledException" />, and the current page stays or, when it
    ///     was already being left, is entered again.
    /// </param>
    /// <returns>
    ///     <see langword="false" /> when the page is already shown with an equal parameter, the page does not accept a
    ///     parameter of type <typeparamref name="TParameter" />, a guard keeps the current page, or entering it failed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="viewModelType" /> or <paramref name="parameter" /> is
    ///     <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="viewModelType" /> does not implement
    ///     <see cref="INavigableViewModel" />.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was canceled.</exception>
    /// <exception cref="ObjectDisposedException">The navigation service was disposed.</exception>
    Task<bool> NavigateToAsync<TParameter>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        Type viewModelType,
        TParameter parameter,
        CancellationToken cancellationToken = default)
        where TParameter : notnull;

    /// <summary>
    ///     Removes all previous and next pages and keeps the current page, for example after signing in or out,
    ///     so going back cannot return to the sign-in page. The removed pages are released without being asked
    ///     or told, because none of them is the current page.
    /// </summary>
    /// <param name="cancellationToken">Cancels waiting for a navigation that is still running.</param>
    /// <returns>A task that completes when the removed pages are released.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was canceled.</exception>
    /// <exception cref="ObjectDisposedException">The navigation service was disposed.</exception>
    Task ClearHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Navigates to the previous page.
    /// </summary>
    /// <param name="cancellationToken">
    ///     Cancels the navigation: it throws <see cref="OperationCanceledException" />, and the current page stays or, when it
    ///     was already being left, is entered again.
    /// </param>
    /// <returns>
    ///     <see langword="false" /> when there is no previous page, a guard keeps the current page, or entering it
    ///     failed.
    /// </returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was canceled.</exception>
    /// <exception cref="ObjectDisposedException">The navigation service was disposed.</exception>
    Task<bool> GoBackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Navigates to the next page after going back.
    /// </summary>
    /// <param name="cancellationToken">
    ///     Cancels the navigation: it throws <see cref="OperationCanceledException" />, and the current page stays or, when it
    ///     was already being left, is entered again.
    /// </param>
    /// <returns><see langword="false" /> when there is no next page, a guard keeps the current page, or entering it failed.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was canceled.</exception>
    /// <exception cref="ObjectDisposedException">The navigation service was disposed.</exception>
    Task<bool> GoForwardAsync(CancellationToken cancellationToken = default);
}
