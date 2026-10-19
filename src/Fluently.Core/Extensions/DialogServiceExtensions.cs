using System.Diagnostics.CodeAnalysis;

namespace Fluently;

/// <summary>
///     Shows dialogs with the dialog type checked at compile time.
/// </summary>
public static class DialogServiceExtensions
{
    /// <param name="dialogs">The dialog service to show the dialog with.</param>
    extension(IDialogService dialogs)
    {
        /// <summary>
        ///     Shows the dialog of view model type <typeparamref name="TDialog" /> and waits until it is closed.
        ///     Completes with <see cref="DialogResult{TResult}.Dismissed" /> when the dialog is dismissed, needs a
        ///     parameter, is already open, or fails to open. Canceling <paramref name="cancellationToken" /> dismisses the
        ///     dialog and throws <see cref="OperationCanceledException" />, unless the dialog was already being closed: then the
        ///     result it closes with is returned.
        ///     The same as <see cref="IDialogService.ShowAsync{TResult}(Type, CancellationToken)" />, with the dialog type
        ///     checked at compile time: a type that is not an <see cref="IDialog{TResult}" /> of <typeparamref name="TResult" />
        ///     does not compile, so this call never throws <see cref="ArgumentException" />.
        /// </summary>
        /// <typeparam name="TDialog">The type of the dialog view model, registered in the container.</typeparam>
        /// <typeparam name="TResult">The type of the result the dialog returns.</typeparam>
        /// <param name="cancellationToken">Dismisses the dialog when canceled.</param>
        /// <returns>The result the dialog was closed with, or <see cref="DialogResult{TResult}.Dismissed" />.</returns>
        /// <exception cref="OperationCanceledException">
        ///     <paramref name="cancellationToken" /> was canceled before the dialog was
        ///     being closed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The dialog service was disposed.</exception>
        public Task<DialogResult<TResult>> ShowAsync<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
            TDialog, TResult>(
            CancellationToken cancellationToken = default)
            where TDialog : IDialog<TResult> =>
            dialogs.ShowAsync<TResult>(typeof(TDialog), cancellationToken);

        /// <summary>
        ///     Shows the dialog of view model type <typeparamref name="TDialog" /> with <paramref name="parameter" />, which
        ///     it receives through <see cref="INavigatedTo{TParameter}" />, and waits until it is closed.
        ///     Completes with <see cref="DialogResult{TResult}.Dismissed" /> when the dialog is dismissed, is already open, or
        ///     fails to open. Canceling <paramref name="cancellationToken" /> dismisses the dialog and throws
        ///     <see cref="OperationCanceledException" />, unless the dialog was already being closed: then the result it
        ///     closes with is returned.
        ///     The same as <see cref="IDialogService.ShowAsync{TResult, TParameter}(Type, TParameter, CancellationToken)" />,
        ///     with the types checked at compile time: a dialog that does not implement <see cref="IDialog{TResult}" /> and
        ///     <see cref="INavigatedTo{TParameter}" /> for these types does not compile, so this call never throws
        ///     <see cref="ArgumentException" /> and is never dismissed because the dialog does not accept the parameter.
        /// </summary>
        /// <typeparam name="TDialog">The type of the dialog view model, registered in the container.</typeparam>
        /// <typeparam name="TResult">The type of the result the dialog returns.</typeparam>
        /// <typeparam name="TParameter">
        ///     The type of the parameter; <typeparamref name="TDialog" /> implements
        ///     <see cref="INavigatedTo{TParameter}" /> for it.
        /// </typeparam>
        /// <param name="parameter">The parameter passed to the dialog.</param>
        /// <param name="cancellationToken">Dismisses the dialog when canceled.</param>
        /// <returns>The result the dialog was closed with, or <see cref="DialogResult{TResult}.Dismissed" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="parameter" /> is <see langword="null" />.</exception>
        /// <exception cref="OperationCanceledException">
        ///     <paramref name="cancellationToken" /> was canceled before the dialog was
        ///     being closed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The dialog service was disposed.</exception>
        public Task<DialogResult<TResult>> ShowAsync<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
            TDialog, TResult, TParameter>(
            TParameter parameter,
            CancellationToken cancellationToken = default)
            where TDialog : IDialog<TResult>, INavigatedTo<TParameter>
            where TParameter : notnull =>
            dialogs.ShowAsync<TResult, TParameter>(typeof(TDialog), parameter, cancellationToken);
    }
}
