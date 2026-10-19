namespace Fluently;

/// <summary>
/// A base class for toasts with a view of their own that close themselves, for example after an Undo button was
/// clicked: <see cref="Dismiss"/> closes the toast before its duration ends. The toast service that shows the toast
/// provides itself, so the toast needs no constructor parameter for it.
/// <code>
/// public sealed partial class UndoToastViewModel(Action undo) : ToastViewModel
/// {
///     [RelayCommand]
///     private void Undo()
///     {
///         undo();
///         Dismiss();
///     }
/// }
/// </code>
/// It does not implement <see cref="System.ComponentModel.INotifyPropertyChanged"/>: with CommunityToolkit.Mvvm, add
/// <c>[ObservableObject]</c> when the toast has properties that change. Implement <see cref="IToast"/> instead when the
/// toast needs a different base class.
/// </summary>
public abstract class ToastViewModel : IToast, IHostedToast
{
    private IToastService? _toasts;

    IToastService? IHostedToast.Toasts
    {
        set => _toasts = value;
    }

    /// <summary>
    /// Closes this toast before its duration ends.
    /// </summary>
    /// <returns><see langword="true"/> when the toast was closed; <see langword="false"/> when it is not shown.</returns>
    protected bool Dismiss() => _toasts?.Dismiss(this) ?? false;
}
