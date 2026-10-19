namespace Fluently.Gallery.ViewModels;

public sealed class MainWindowViewModel(INavigationService navigation, IDialogService dialogs, IToastService toasts)
{
    public INavigationService Navigation { get; } = navigation;

    public IDialogService Dialogs { get; } = dialogs;

    public IToastService Toasts { get; } = toasts;
}
