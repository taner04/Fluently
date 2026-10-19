using CommunityToolkit.Mvvm.Input;

namespace Fluently.Gallery.ViewModels;

[TransientPage]
public sealed partial class ToastsViewModel(IToastService toasts) : INavigableViewModel
{
    public IReadOnlyList<ToastPosition> Positions { get; } = Enum.GetValues<ToastPosition>();

    public ToastPosition Position
    {
        get => toasts.Position;
        set => toasts.Position = value;
    }

    [RelayCommand]
    private void ShowInformation() => toasts.ShowInformation("Sync started", "Your files are being synchronized.");

    [RelayCommand]
    private void ShowSuccess() => toasts.ShowSuccess("Saved", "Your changes were saved.");

    [RelayCommand]
    private void ShowWarning() =>
        toasts.ShowWarning("Low disk space", "Less than 1 GB is left on this drive.");

    [RelayCommand]
    private void ShowError() =>
        toasts.ShowError("Upload failed", "The server could not be reached.");

    [RelayCommand]
    private void ShowSticky() =>
        toasts.ShowWarning("Offline", "This toast stays until you close it.", Timeout.InfiniteTimeSpan);
}
