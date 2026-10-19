namespace Fluently;

/// <summary>
///     Data for the <see cref="INavigationService.Navigated" /> event.
/// </summary>
/// <param name="page">The page that was navigated to.</param>
public sealed class NavigatedEventArgs(INavigableViewModel page) : EventArgs
{
    /// <summary>
    ///     The page that was navigated to.
    /// </summary>
    public INavigableViewModel Page { get; } = page;
}
