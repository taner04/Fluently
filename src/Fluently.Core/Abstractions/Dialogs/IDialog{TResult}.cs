namespace Fluently;

/// <summary>
///     A dialog that returns a result of type <typeparamref name="TResult" />.
///     Show it with <see cref="DialogServiceExtensions.ShowAsync{TDialog, TResult}(IDialogService, CancellationToken)" />
///     and close it with
///     <see cref="IDialogService.CloseAsync{TResult}" />. Like a page, it can implement <see cref="INavigatedTo" />,
///     <see cref="INavigatedTo{TParameter}" /> and <see cref="INavigatedFrom" /> to react to being opened and closed.
/// </summary>
/// <typeparam name="TResult">The type of the result the dialog returns.</typeparam>
public interface IDialog<TResult> : IDialog;
