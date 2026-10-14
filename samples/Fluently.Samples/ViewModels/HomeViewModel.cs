using CommunityToolkit.Mvvm.Input;

namespace Fluently.Samples.ViewModels;

public sealed partial class HomeViewModel(INavigationService navigation) : INavigableViewModel
{
    public IReadOnlyList<int> Items { get; } = Enumerable.Range(1, 5).ToList();

    [RelayCommand]
    private Task OpenAsync(int item) => navigation.NavigateToAsync<DetailsViewModel, int>(item);
}
