using Fluently.Gallery.ViewModels;

namespace Fluently.Gallery.Views;

internal partial class ToastsView : ViewFor<ToastsViewModel>
{
    public ToastsView(ToastsViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
