using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fluently.Gallery.Models;

namespace Fluently.Gallery.ViewModels;

[TransientPage]
public sealed partial class DialogsViewModel(IDialogService dialogs) : ObservableObject, INavigableViewModel
{
    [ObservableProperty] public partial string LastResult { get; private set; } = "No dialog shown yet.";

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        var request = new ConfirmRequest("Send the report?", "The report goes to everyone on the team.", "Send");
        var result = await dialogs.ShowAsync<ConfirmDialogViewModel, bool, ConfirmRequest>(request);
        LastResult = result.IsCompleted ? $"Confirm returned {result.Value}." : "Confirm was dismissed.";
    }

    [RelayCommand]
    private async Task RenameAsync()
    {
        var result = await dialogs.ShowAsync<InputDialogViewModel, string, string>("Rename the project");
        LastResult = result.IsCompleted ? $"Input returned \"{result.Value}\"." : "Input was dismissed.";
    }
}
