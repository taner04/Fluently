using Avalonia.Controls;

namespace Fluently.Avalonia.UnitTests.TestDoubles.Views;

public sealed class DetailsView(DetailsViewModel viewModel) : UserControl, IViewFor<DetailsViewModel>
{
    public DetailsViewModel ViewModel { get; } = viewModel;
}
