using Fluently.Gallery.ViewModels;

namespace Fluently.Gallery.Views;

internal partial class DetailsView : ViewFor<DetailsViewModel>
{
    public DetailsView(DetailsViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
