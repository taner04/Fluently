using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Fluently;

/// <summary>
///     Shows <see cref="IDialog{TResult}" /> view models on top of the current page and returns their result.
///     Dialogs form a stack, so a dialog can show another dialog. Every dialog resolves from its own service scope,
///     which is disposed together with its disposable services when the dialog closes.
///     <see cref="Dialogs" /> and <see cref="CurrentDialog" /> raise <see cref="INotifyPropertyChanged.PropertyChanged" />
///     ,
///     so a dialog host can bind to them directly.
/// </summary>
public interface IDialogService : INotifyPropertyChanged
{
    /// <summary>
    ///     The open dialogs, from the first one shown to the one on top.
    /// </summary>
    IReadOnlyList<IDialog> Dialogs { get; }

    /// <summary>
    ///     The dialog on top, or <see langword="null" /> when no dialog is open.
    /// </summary>
    IDialog? CurrentDialog { get; }

    /// <summary>
    ///     Shows the dialog of view model type <paramref name="dialogType" /> and waits until it is closed.
    ///     Completes with <see cref="DialogResult{TResult}.Dismissed" /> when the dialog is dismissed, needs a parameter,
    ///     is already open, or fails to open. Canceling <paramref name="cancellationToken" /> dismisses the dialog and
    ///     throws <see cref="OperationCanceledException" />, unless the dialog was already being closed: then the result it
    ///     closes with is returned.
    ///     <see cref="DialogServiceExtensions.ShowAsync{TDialog, TResult}(IDialogService, CancellationToken)" /> is the
    ///     same with the dialog type checked at compile time.
    /// </summary>
    /// <typeparam name="TResult">The type of the result the dialog returns.</typeparam>
    /// <param name="dialogType">The type of the dialog view model, registered in the container.</param>
    /// <param name="cancellationToken">Dismisses the dialog when canceled.</param>
    /// <returns>The result the dialog was closed with, or <see cref="DialogResult{TResult}.Dismissed" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dialogType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="dialogType" /> does not implement <see cref="IDialog{TResult}" />.</exception>
    /// <exception cref="OperationCanceledException">
    ///     <paramref name="cancellationToken" /> was canceled before the dialog was
    ///     being closed.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The dialog service was disposed.</exception>
    Task<DialogResult<TResult>> ShowAsync<TResult>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        Type dialogType,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Shows the dialog of view model type <paramref name="dialogType" /> with <paramref name="parameter" />, which it
    ///     receives through <see cref="INavigatedTo{TParameter}" />, and waits until it is closed. Also completes with
    ///     <see cref="DialogResult{TResult}.Dismissed" /> when the dialog does not accept a parameter of type
    ///     <typeparamref name="TParameter" />; otherwise behaves like
    ///     <see cref="ShowAsync{TResult}(Type, CancellationToken)" />.
    ///     <see
    ///         cref="DialogServiceExtensions.ShowAsync{TDialog, TResult, TParameter}(IDialogService, TParameter, CancellationToken)" />
    ///     is the same with the dialog type checked at compile time.
    /// </summary>
    /// <typeparam name="TResult">The type of the result the dialog returns.</typeparam>
    /// <typeparam name="TParameter">
    ///     The type of the parameter; the dialog receives it through
    ///     <see cref="INavigatedTo{TParameter}" />.
    /// </typeparam>
    /// <param name="dialogType">The type of the dialog view model, registered in the container.</param>
    /// <param name="parameter">The parameter passed to the dialog.</param>
    /// <param name="cancellationToken">Dismisses the dialog when canceled.</param>
    /// <returns>The result the dialog was closed with, or <see cref="DialogResult{TResult}.Dismissed" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="dialogType" /> or <paramref name="parameter" /> is
    ///     <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="dialogType" /> does not implement <see cref="IDialog{TResult}" />.</exception>
    /// <exception cref="OperationCanceledException">
    ///     <paramref name="cancellationToken" /> was canceled before the dialog was
    ///     being closed.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The dialog service was disposed.</exception>
    Task<DialogResult<TResult>> ShowAsync<TResult, TParameter>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        Type dialogType,
        TParameter parameter,
        CancellationToken cancellationToken = default)
        where TParameter : notnull;

    /// <summary>
    ///     Closes <paramref name="dialog" /> with <paramref name="result" />, which the call that showed it returns. This also
    ///     works while the dialog is still being opened, for example from its own <see cref="INavigatedTo" />.
    ///     Returns <see langword="false" /> when the dialog is neither open nor being opened.
    /// </summary>
    /// <typeparam name="TResult">The type of the result the dialog returns.</typeparam>
    /// <param name="dialog">The open dialog to close, usually the dialog view model itself.</param>
    /// <param name="result">The result the call that showed the dialog returns.</param>
    /// <returns><see langword="true" /> when the dialog was closed; <see langword="false" /> when it is not open.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dialog" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The dialog was shown for a different result type.</exception>
    Task<bool> CloseAsync<TResult>(IDialog<TResult> dialog, TResult result);

    /// <summary>
    ///     Closes <paramref name="dialog" /> without a result. It always closes, without asking
    ///     <see cref="ICanNavigateFrom" />; use <see cref="RequestDismissAsync" /> when the user asks for it. This also works
    ///     while the dialog is still being opened. Returns <see langword="false" /> when the dialog is neither open nor being
    ///     opened.
    /// </summary>
    /// <param name="dialog">The open dialog to close.</param>
    /// <returns><see langword="true" /> when the dialog was dismissed; <see langword="false" /> when it is not open.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dialog" /> is <see langword="null" />.</exception>
    Task<bool> DismissAsync(IDialog dialog);

    /// <summary>
    ///     Asks <paramref name="dialog" /> to close without a result because the user wants it gone, for example by
    ///     pressing Escape. A dialog that implements <see cref="ICanNavigateFrom" /> can refuse, and may show another
    ///     dialog to ask first. Returns <see langword="false" /> when the dialog refused, its guard threw, it is not open,
    ///     or a request for it is already running.
    /// </summary>
    /// <param name="dialog">The open dialog the user wants to close.</param>
    /// <param name="cancellationToken">Passed to the dialog's guard; canceling it keeps the dialog open and throws.</param>
    /// <returns><see langword="true" /> when the dialog was dismissed; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dialog" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was canceled.</exception>
    Task<bool> RequestDismissAsync(IDialog dialog, CancellationToken cancellationToken = default);
}
