using Fluently.Gallery.ViewModels;

namespace Fluently.Gallery.Views;

internal partial class ItemsView : ViewFor<ItemsViewModel>
{
    public ItemsView(ItemsViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
