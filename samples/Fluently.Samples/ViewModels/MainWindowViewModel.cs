using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Fluently.Samples.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(INavigationService navigation)
    {
        Navigation = navigation;
        Navigation.Navigated += (_, _) => GoBackCommand.NotifyCanExecuteChanged();
    }

    public INavigationService Navigation { get; }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private Task GoBackAsync() => Navigation.GoBackAsync();

    private bool CanGoBack() => Navigation.CanGoBack;
}
