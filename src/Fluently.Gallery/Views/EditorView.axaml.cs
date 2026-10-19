using Fluently.Gallery.ViewModels;

namespace Fluently.Gallery.Views;

internal partial class EditorView : ViewFor<EditorViewModel>
{
    public EditorView(EditorViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
