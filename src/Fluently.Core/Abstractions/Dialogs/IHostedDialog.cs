namespace Fluently;

/// <summary>
/// A dialog that remembers the dialog service showing it, so it can close itself: <see cref="DialogViewModel{TResult}"/>.
/// </summary>
internal interface IHostedDialog
{
    IDialogService? Dialogs { set; }
}
