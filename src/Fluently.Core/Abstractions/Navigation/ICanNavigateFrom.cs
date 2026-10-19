namespace Fluently;

/// <summary>
///     A page that can prevent leaving it, for example while it has unsaved changes.
///     A dialog can implement it too, to refuse or confirm <see cref="IDialogService.RequestDismissAsync" />.
/// </summary>
public interface ICanNavigateFrom : INavigableViewModel
{
    /// <summary>
    ///     Called before the page is left, by navigating to another page or through the history.
    ///     Returning <see langword="false" /> or throwing keeps the page and the navigation returns <see langword="false" />.
    ///     The guard may show a dialog to ask first.
    /// </summary>
    /// <param name="cancellationToken">Canceled when the navigation that asks is canceled.</param>
    /// <returns><see langword="true" /> to let the page be left; <see langword="false" /> to keep it.</returns>
    Task<bool> CanNavigateFromAsync(CancellationToken cancellationToken);
}
