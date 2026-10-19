using Fluently.Gallery.ViewModels;

namespace Fluently.Gallery.Views;

internal partial class ConfirmDialogView : ViewFor<ConfirmDialogViewModel>
{
    public ConfirmDialogView(ConfirmDialogViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
