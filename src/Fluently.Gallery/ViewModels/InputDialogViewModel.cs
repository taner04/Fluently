using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fluently.Gallery.Models;

namespace Fluently.Gallery.ViewModels;

[Dialog]
[ObservableObject]
public sealed partial class InputDialogViewModel(IDialogService dialogs)
    : DialogViewModel<string>, INavigatedTo<string>, ICanNavigateFrom
{
    [ObservableProperty] public partial string Title { get; private set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
    public partial string Text { get; set; } = "";

    public async Task<bool> CanNavigateFromAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Text))
        {
            return true;
        }

        var request = new ConfirmRequest("Discard input?", "What you typed will be lost.", "Discard");
        var result = await dialogs.ShowAsync<ConfirmDialogViewModel, bool, ConfirmRequest>(request, cancellationToken);
        return result.Value;
    }

    public Task OnNavigatedToAsync(string parameter, CancellationToken cancellationToken)
    {
        Title = parameter;
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanAccept))]
    private Task AcceptAsync() => CloseAsync(Text.Trim());

    [RelayCommand]
    private Task CancelAsync() => DismissAsync();

    private bool CanAccept() => !string.IsNullOrWhiteSpace(Text);
}
