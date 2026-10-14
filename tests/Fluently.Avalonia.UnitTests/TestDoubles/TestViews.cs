using Avalonia.Controls;

namespace Fluently.Avalonia.UnitTests.TestDoubles;

public sealed class HomeViewModel : INavigableViewModel;

public sealed class DetailsViewModel : INavigableViewModel;

public sealed class ViewModelWithoutView : INavigableViewModel;

public sealed class HomeView(HomeViewModel viewModel) : UserControl, IViewFor<HomeViewModel>
{
    public HomeViewModel ViewModel { get; } = viewModel;
}

public sealed class DetailsView(DetailsViewModel viewModel) : UserControl, IViewFor<DetailsViewModel>
{
    public DetailsViewModel ViewModel { get; } = viewModel;
}
