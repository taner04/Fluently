using Fluently.Gallery.ViewModels;

namespace Fluently.Gallery.Views;

internal partial class UndoToastView : ViewFor<UndoToastViewModel>
{
    public UndoToastView(UndoToastViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
