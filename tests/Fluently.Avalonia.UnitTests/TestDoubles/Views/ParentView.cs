using Avalonia.Controls;

namespace Fluently.Avalonia.UnitTests.TestDoubles.Views;

public sealed class ParentView : ViewFor<ParentViewModel>
{
    public ParentView(ParentViewModel viewModel) : base(viewModel)
    {
        Content = new ContentControl { Content = viewModel.Child };
    }
}
