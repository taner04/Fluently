using CommunityToolkit.Mvvm.Input;

namespace Fluently.Samples.ViewModels;

public sealed partial class MainWindowViewModel(INavigationService navigation)
{
    public INavigationService Navigation { get; } = navigation;

    [RelayCommand]
    private Task GoBackAsync() => Navigation.GoBackAsync();
}
