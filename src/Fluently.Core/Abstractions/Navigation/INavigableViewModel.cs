namespace Fluently;

/// <summary>
///     A view model that can be navigated to by an <see cref="INavigationService" />.
///     Implement <see cref="INavigatedTo" />, <see cref="INavigatedTo{TParameter}" />, <see cref="INavigatedFrom" /> or
///     <see cref="ICanNavigateFrom" /> instead to react to navigation; they all derive from it.
/// </summary>
public interface INavigableViewModel;
