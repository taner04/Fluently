using System.ComponentModel;

namespace Fluently;

internal sealed partial class NavigationService
{
    private static readonly PropertyChangedEventArgs CurrentPageChanged = new(nameof(CurrentPage));
    private static readonly PropertyChangedEventArgs CanGoBackChanged = new(nameof(CanGoBack));
    private static readonly PropertyChangedEventArgs CanGoForwardChanged = new(nameof(CanGoForward));

    public event PropertyChangedEventHandler? PropertyChanged;

    // Taken while the transition lock is held, so it describes the state the change started from.
    private HistoryState CaptureState() => new(CurrentPage, CanGoBack, CanGoForward);

    // Raised after the transition lock is released, like Navigated, so handlers can navigate again.
    private void RaisePropertyChanges(HistoryState before)
    {
        if (!ReferenceEquals(before.CurrentPage, CurrentPage))
        {
            PropertyChanged?.Invoke(this, CurrentPageChanged);
        }

        if (before.CanGoBack != CanGoBack)
        {
            PropertyChanged?.Invoke(this, CanGoBackChanged);
        }

        if (before.CanGoForward != CanGoForward)
        {
            PropertyChanged?.Invoke(this, CanGoForwardChanged);
        }
    }
}
