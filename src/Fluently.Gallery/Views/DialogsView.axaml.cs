using Fluently.Gallery.ViewModels;

namespace Fluently.Gallery.Views;

internal partial class DialogsView : ViewFor<DialogsViewModel>
{
    public DialogsView(DialogsViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
