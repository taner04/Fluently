using Avalonia.Controls;
using Fluently.Samples.ViewModels;

namespace Fluently.Samples.Views;

internal partial class HomeView : UserControl, IViewFor<HomeViewModel>
{
    public HomeView(HomeViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public HomeViewModel ViewModel { get; }
}
