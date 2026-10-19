using Fluently.Gallery.ViewModels;

namespace Fluently.Gallery.Views;

internal partial class HomeView : ViewFor<HomeViewModel>
{
    public HomeView(HomeViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
