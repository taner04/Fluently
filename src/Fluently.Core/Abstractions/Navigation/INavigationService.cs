using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Fluently;

/// <summary>
/// Navigates between <see cref="INavigableViewModel"/> pages and keeps a back and forward history.
/// Every history entry resolves its page from its own service scope, so a transient page is created
/// on every navigation, and the scope is disposed together with its disposable services when the entry
/// leaves the history. Disposing the service releases all entries; prefer <see cref="IAsyncDisposable.DisposeAsync"/>,
/// which also disposes services that can only be disposed asynchronously.
/// <see cref="CurrentPage"/>, <see cref="CanGoBack"/> and <see cref="CanGoForward"/> raise
/// <see cref="INotifyPropertyChanged.PropertyChanged"/>, so views can bind to them directly.
/// </summary>
public interface INavigationService : INotifyPropertyChanged, IDisposable, IAsyncDisposable
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
    /// Raised after a navigation completed, after the changed properties raised
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/>.
    /// </summary>
    event EventHandler<NavigatedEventArgs>? Navigated;

    /// <summary>
    /// Navigates to the page of view model type <typeparamref name="TPage"/> without a parameter.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when the page is already shown, a guard keeps the current page,
    /// the page needs a parameter, or entering it failed.
    /// </returns>
    Task<bool> NavigateToAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TPage>(
        CancellationToken cancellationToken = default)
        where TPage : INavigableViewModel;

    /// <summary>
    /// Navigates to the page of view model type <typeparamref name="TPage"/> and passes it a parameter.
    /// Navigating to the current page with a different parameter replaces the current history entry.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when the page is already shown with an equal parameter,
    /// a guard keeps the current page, or entering it failed.
    /// </returns>
    Task<bool> NavigateToAsync<TPage, TParameter>(
        TParameter parameter,
        CancellationToken cancellationToken = default)
        where TPage : INavigatedTo<TParameter>
        where TParameter : notnull;

    /// <summary>
    /// Navigates to the page of the given view model type without a parameter,
    /// for callers that only know the type at run time, such as menus.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when the page is already shown, a guard keeps the current page,
    /// the page needs a parameter, or entering it failed.
    /// </returns>
    Task<bool> NavigateToAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type viewModelType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all previous and next pages and keeps the current page, for example after signing in or out,
    /// so going back cannot return to the sign-in page. The removed pages are released without being asked
    /// or told, because none of them is the current page.
    /// </summary>
    Task ClearHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Navigates to the previous page.
    /// </summary>
    /// <returns><see langword="false"/> when there is no previous page, a guard keeps the current page, or entering it failed.</returns>
    Task<bool> GoBackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Navigates to the next page after going back.
    /// </summary>
    /// <returns><see langword="false"/> when there is no next page, a guard keeps the current page, or entering it failed.</returns>
    Task<bool> GoForwardAsync(CancellationToken cancellationToken = default);
}
