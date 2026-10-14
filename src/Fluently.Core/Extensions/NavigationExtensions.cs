namespace Fluently;

/// <summary>
/// Generic overloads for <see cref="INavigationService"/>.
/// </summary>
public static class NavigationExtensions
{
    extension(INavigationService navigationService)
    {
        /// <summary>
        /// Navigates to the page of view model type <typeparamref name="T"/>.
        /// </summary>
        public Task<bool> NavigateToAsync<T>(CancellationToken cancellationToken = default)
            where T : INavigableViewModel =>
            navigationService.NavigateToAsync(typeof(T), cancellationToken);

        /// <summary>
        /// Navigates to the page of view model type <typeparamref name="T"/> and passes it a parameter.
        /// </summary>
        public Task<bool> NavigateToAsync<T>(object parameter, CancellationToken cancellationToken = default)
            where T : INavigableViewModel =>
            navigationService.NavigateToAsync(typeof(T), parameter, cancellationToken);

        /// <summary>
        /// Navigates to the page of view model type <typeparamref name="TPage"/> and passes it a parameter
        /// of the type the page expects.
        /// </summary>
        public Task<bool> NavigateToAsync<TPage, TParameter>(
            TParameter parameter,
            CancellationToken cancellationToken = default)
            where TPage : INavigatedTo<TParameter>
            where TParameter : notnull =>
            navigationService.NavigateToAsync(typeof(TPage), parameter, cancellationToken);
    }
}
