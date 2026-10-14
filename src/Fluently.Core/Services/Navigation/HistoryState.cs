namespace Fluently;

/// <summary>
/// The observable state of a navigation service, compared before and after a change to raise only the
/// properties that changed.
/// </summary>
internal readonly record struct HistoryState(INavigableViewModel? CurrentPage, bool CanGoBack, bool CanGoForward);
