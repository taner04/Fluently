using System.Diagnostics.CodeAnalysis;

namespace Fluently;

/// <summary>
///     Navigates with the page type checked at compile time.
/// </summary>
public static class NavigationServiceExtensions
{
    /// <param name="navigation">The navigation service to navigate with.</param>
    extension(INavigationService navigation)
    {
        /// <summary>
        ///     Navigates to the page of view model type <typeparamref name="TPage" /> without a parameter.
        ///     The same as <see cref="INavigationService.NavigateToAsync(Type, CancellationToken)" />, with the page type
        ///     checked at compile time: a type that is not an <see cref="INavigableViewModel" /> does not compile, so this
        ///     call never throws <see cref="ArgumentException" />.
        /// </summary>
        /// <typeparam name="TPage">The type of the page view model, registered in the container.</typeparam>
        /// <param name="cancellationToken">
        ///     Cancels the navigation: it throws <see cref="OperationCanceledException" />, and the current page stays or,
        ///     when it was already being left, is entered again.
        /// </param>
        /// <returns>
        ///     <see langword="false" /> when the page is already shown, a guard keeps the current page,
        ///     the page needs a parameter, or entering it failed.
        /// </returns>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was canceled.</exception>
        /// <exception cref="ObjectDisposedException">The navigation service was disposed.</exception>
        public Task<bool> NavigateToAsync<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
            TPage>(
            CancellationToken cancellationToken = default)
            where TPage : INavigableViewModel =>
            navigation.NavigateToAsync(typeof(TPage), cancellationToken);

        /// <summary>
        ///     Navigates to the page of view model type <typeparamref name="TPage" /> and passes it
        ///     <paramref name="parameter" /> through <see cref="INavigatedTo{TParameter}" />.
        ///     Navigating to the current page with a different parameter replaces the current history entry.
        ///     The same as <see cref="INavigationService.NavigateToAsync{TParameter}(Type, TParameter, CancellationToken)" />,
        ///     with the types checked at compile time: a page that does not implement
        ///     <see cref="INavigatedTo{TParameter}" /> for <typeparamref name="TParameter" /> does not compile, so this call
        ///     never throws <see cref="ArgumentException" /> and never returns <see langword="false" /> because the page does
        ///     not accept the parameter.
        /// </summary>
        /// <typeparam name="TPage">The type of the page view model, registered in the container.</typeparam>
        /// <typeparam name="TParameter">
        ///     The type of the parameter; <typeparamref name="TPage" /> implements
        ///     <see cref="INavigatedTo{TParameter}" /> for it.
        /// </typeparam>
        /// <param name="parameter">The parameter passed to the page.</param>
        /// <param name="cancellationToken">
        ///     Cancels the navigation: it throws <see cref="OperationCanceledException" />, and the current page stays or,
        ///     when it was already being left, is entered again.
        /// </param>
        /// <returns>
        ///     <see langword="false" /> when the page is already shown with an equal parameter,
        ///     a guard keeps the current page, or entering it failed.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="parameter" /> is <see langword="null" />.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was canceled.</exception>
        /// <exception cref="ObjectDisposedException">The navigation service was disposed.</exception>
        public Task<bool> NavigateToAsync<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
            TPage, TParameter>(
            TParameter parameter,
            CancellationToken cancellationToken = default)
            where TPage : INavigatedTo<TParameter>
            where TParameter : notnull =>
            navigation.NavigateToAsync(typeof(TPage), parameter, cancellationToken);
    }
}
