using Fluently.Gallery.ViewModels;

namespace Fluently.Gallery.Views;

internal partial class InputDialogView : ViewFor<InputDialogViewModel>
{
    public InputDialogView(InputDialogViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
