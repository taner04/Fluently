namespace Fluently;

/// <summary>
/// A base class for dialogs that close themselves: <see cref="CloseAsync"/> returns a result to the call that showed
/// the dialog, <see cref="DismissAsync"/> closes it without one, and <see cref="RequestDismissAsync"/> asks its
/// <see cref="ICanNavigateFrom"/> guard first. The dialog service that shows the dialog provides itself, so the
/// dialog needs no constructor parameter for it.
/// <code>
/// [Dialog]
/// [ObservableObject]
/// public sealed partial class RenameViewModel : DialogViewModel&lt;string&gt;
/// {
///     [ObservableProperty] public partial string Name { get; set; } = "";
///
///     [RelayCommand] private Task SaveAsync() => CloseAsync(Name);
/// }
/// </code>
/// It does not implement <see cref="System.ComponentModel.INotifyPropertyChanged"/>, so Fluently does not depend on
/// an MVVM library: with CommunityToolkit.Mvvm, add <c>[ObservableObject]</c> as above; otherwise implement it
/// yourself. Implement <see cref="IDialog{TResult}"/> instead when the dialog needs a different base class.
/// </summary>
/// <typeparam name="TResult">The type of the result the dialog returns.</typeparam>
public abstract class DialogViewModel<TResult> : IDialog<TResult>, IHostedDialog
{
    private IDialogService? _dialogs;

    IDialogService? IHostedDialog.Dialogs
    {
        set => _dialogs = value;
    }

    /// <summary>
    /// Closes this dialog with <paramref name="result"/>, which the call that showed it returns.
    /// </summary>
    /// <param name="result">The result the call that showed the dialog returns.</param>
    /// <returns><see langword="true"/> when the dialog was closed; <see langword="false"/> when it is not open.</returns>
    protected Task<bool> CloseAsync(TResult result) =>
        _dialogs?.CloseAsync(this, result) ?? Task.FromResult(false);

    /// <summary>
    /// Closes this dialog without a result, without asking its <see cref="ICanNavigateFrom"/> guard.
    /// </summary>
    /// <returns><see langword="true"/> when the dialog was dismissed; <see langword="false"/> when it is not open.</returns>
    protected Task<bool> DismissAsync() => _dialogs?.DismissAsync(this) ?? Task.FromResult(false);

    /// <summary>
    /// Closes this dialog without a result unless its <see cref="ICanNavigateFrom"/> guard refuses, like pressing
    /// Escape does.
    /// </summary>
    /// <param name="cancellationToken">Passed to the guard; canceling it keeps the dialog open and throws.</param>
    /// <returns><see langword="true"/> when the dialog was dismissed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    protected Task<bool> RequestDismissAsync(CancellationToken cancellationToken = default) =>
        _dialogs?.RequestDismissAsync(this, cancellationToken) ?? Task.FromResult(false);
}
