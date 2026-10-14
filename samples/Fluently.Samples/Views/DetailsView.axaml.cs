using Avalonia.Controls;
using Fluently.Samples.ViewModels;

namespace Fluently.Samples.Views;

internal partial class DetailsView : UserControl, IViewFor<DetailsViewModel>
{
    public DetailsView(DetailsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public DetailsViewModel ViewModel { get; }
}
