using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fluently.Gallery.Models;

namespace Fluently.Gallery.ViewModels;

[Dialog]
[ObservableObject]
public sealed partial class ConfirmDialogViewModel : DialogViewModel<bool>, INavigatedTo<ConfirmRequest>
{
    [ObservableProperty] public partial ConfirmRequest? Request { get; private set; }

    public Task OnNavigatedToAsync(ConfirmRequest parameter, CancellationToken cancellationToken)
    {
        Request = parameter;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ConfirmAsync() => CloseAsync(true);

    [RelayCommand]
    private Task CancelAsync() => CloseAsync(false);
}
