using Avalonia.Controls;

namespace Fluently.Avalonia.UnitTests.TestDoubles.Views;

public sealed class HomeView(HomeViewModel viewModel) : UserControl, IViewFor<HomeViewModel>
{
    public HomeViewModel ViewModel { get; } = viewModel;
}
