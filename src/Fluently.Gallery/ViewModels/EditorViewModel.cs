using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fluently.Gallery.Models;

namespace Fluently.Gallery.ViewModels;

[TransientPage]
public sealed partial class EditorViewModel(IDialogService dialogs) : ObservableObject, ICanNavigateFrom
{
    private string _saved = "Fluently keeps this page while it has unsaved changes.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanges))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Text { get; set; } = "Fluently keeps this page while it has unsaved changes.";

    public bool HasChanges => Text != _saved;

    public async Task<bool> CanNavigateFromAsync(CancellationToken cancellationToken)
    {
        if (!HasChanges)
        {
            return true;
        }

        var request = new ConfirmRequest("Discard changes?", "Your edits on this page have not been saved.", "Discard");
        var result = await dialogs.ShowAsync<ConfirmDialogViewModel, bool, ConfirmRequest>(request, cancellationToken);
        if (result.Value)
        {
            Text = _saved;
        }

        return result.Value;
    }

    [RelayCommand(CanExecute = nameof(HasChanges))]
    private void Save()
    {
        _saved = Text;
        OnPropertyChanged(nameof(HasChanges));
        SaveCommand.NotifyCanExecuteChanged();
    }
}
