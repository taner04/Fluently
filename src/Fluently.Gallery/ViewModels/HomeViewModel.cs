using CommunityToolkit.Mvvm.Input;

namespace Fluently.Gallery.ViewModels;

[SingletonPage]
public sealed partial class HomeViewModel(INavigationService navigation) : INavigableViewModel
{
    [RelayCommand]
    private Task OpenAsync(Type page) => navigation.NavigateToAsync(page);
}
