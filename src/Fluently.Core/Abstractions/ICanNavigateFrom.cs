namespace Fluently;

/// <summary>
/// A page that can prevent leaving it, for example while it has unsaved changes.
/// </summary>
public interface ICanNavigateFrom : INavigableViewModel
{
    /// <summary>
    /// Called before the page is left, by navigating to another page or through the history.
    /// Returning <see langword="false"/> or throwing keeps the page and the navigation returns <see langword="false"/>.
    /// </summary>
    Task<bool> CanNavigateFromAsync(CancellationToken cancellationToken);
}
