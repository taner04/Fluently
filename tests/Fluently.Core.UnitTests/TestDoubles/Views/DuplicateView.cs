namespace Fluently.Core.UnitTests.TestDoubles.Views;

internal sealed class DuplicateView(DuplicateViewModel viewModel) : IViewFor<DuplicateViewModel>
{
    public DuplicateViewModel ViewModel { get; } = viewModel;
}
